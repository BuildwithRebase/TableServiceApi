using System;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using MySql.Data.MySqlClient;
using TableService.Core.Models;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using TableServiceApi.ViewModels;
using TableService.Core.Messages;
using TableService.Core.Utility;
using TableService.Core.Exceptions;
using TableService.Core.Security;

namespace TableService.Core.Services
{
    public class SubscriberService
    {
        private bool _tableMapped = false;

        public async Task<SubscribeResponse> Subscribe(SubscribeUserRequest subscribeUserRequest)
        {
            if (!subscribeUserRequest.Password.Equals(subscribeUserRequest.ConfirmPassword))
            {
                throw new MyHttpException(400, "Passwords do not match");
            }

            Team team = await GetTeamByIdAsync(subscribeUserRequest.TeamId);
            if (team == null)
            {
                throw new MyHttpException(400, "Unable to find team");
            }

            Subscriber subscriber = new Subscriber
            {
                TeamId = team.Id,
                Email = subscribeUserRequest.Email,
                FirstName = subscribeUserRequest.FirstName,
                LastName = subscribeUserRequest.LastName,
                PasswordHash = PasswordUtility.HashPassword(subscribeUserRequest.Password),
                Subscribed = true,
                SubscribedAt = DateTime.Now
            };
            subscriber.CreatedUserName = subscriber.Email;
            subscriber.UpdatedUserName = subscriber.UpdatedUserName;
            subscriber.CreatedAt = subscriber.CreatedAt;
            subscriber.UpdatedAt = subscriber.UpdatedAt;

            SetTableForTeam(team);
            int id = await CreateSubscriber(subscriber, team);

            return new SubscribeResponse(id, subscribeUserRequest.Email);
        }

        public async Task<bool> Unsubscribe(UnsubscribeUserRequest request)
        {
            Team team = await GetTeamByIdAsync(request.TeamId);
            if (team == null)
            {
                throw new MyHttpException(400, "Unable to find team");
            }

            SetTableForTeam(team);
            Subscriber subscriber = await GetSubscriberByEmail(team, request.Email);
            subscriber.SessionToken = "";
            subscriber.Subscribed = false;
            subscriber.UpdatedUserName = request.Email;
            subscriber.UpdatedAt = DateTime.Now;

            if (!await UpdateSubscriber(subscriber, team))
            {
                throw new MyHttpException(500, "Unable to update subscriber record");
            }

            return true;
        }

        public async Task<LoginResponse> Login(SubscriberLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                throw new MyHttpException(400, "Email and Password are required");
            }

            Team team = await GetTeamByIdAsync(request.TeamId);
            if (team == null)
            {
                throw new MyHttpException(400, "Unable to find team");
            }

            SetTableForTeam(team);
            Subscriber subscriber = await GetSubscriberByEmail(team, request.Email);
            if (subscriber.Locked)
            {
                throw new MyHttpException(401, "Account locked");
            }
            if (!subscriber.Subscribed)
            {
                throw new MyHttpException(401, "User is unsubscribed");
            }
            if (!PasswordUtility.VerifyPassword(request.Password, subscriber.PasswordHash))
            {
                subscriber.LoginAttempts++;
                subscriber.Locked = (subscriber.LoginAttempts >= 5);

                await UpdateSubscriber(subscriber, team);

                throw new MyHttpException(401, "Invalid Email or Password");
            }

            subscriber.SessionToken = GuidHelper.CreateCryptographicallySecureGuid().ToString();
            subscriber.SessionTokenExpiry = DateTime.Now.AddMinutes(30);
            subscriber.LastAccessedAt = DateTime.Now;
            subscriber.Locked = false;
            subscriber.LoginAttempts = 0;

            await UpdateSubscriber(subscriber, team);

            var claimsIdentity = ClaimsIdentityFactory.ClaimsIdentityFromSubscriber(subscriber);
            var jwt = JwtUtility.GenerateToken(claimsIdentity);

            return new LoginResponse(jwt);
        }

        public async Task<ChangePasswordResponse> ChangePassword(SubscriberChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.CurrentPassword) ||
                string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.NewPasswordConfirm))
            {
                throw new MyHttpException(400, "Email, CurrentPassword, NewPassword and NewPasswordConfirm are required");
            }
            Team team = await GetTeamByIdAsync(request.TeamId);
            if (team == null)
            {
                throw new MyHttpException(400, "Unable to find team");
            }
            if (!request.NewPassword.Equals(request.NewPassword))
            {
                throw new MyHttpException(400, "Passwords do not match");
            }

            SetTableForTeam(team);
            Subscriber subscriber = await GetSubscriberByEmail(team, request.Email);
            if (subscriber.Locked)
            {
                throw new MyHttpException(401, "Account locked");
            }
            if (!PasswordUtility.VerifyPassword(request.CurrentPassword, subscriber.PasswordHash))
            {
                throw new MyHttpException(401, "Invalid Email or CurrentPassword");
            }

            subscriber.PasswordHash = PasswordUtility.HashPassword(request.NewPassword);
            subscriber.Locked = false;
            subscriber.LoginAttempts = 0;

            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            await UpdateSubscriber(subscriber, team);

            return new ChangePasswordResponse(subscriber.Id, team.Id, subscriber.Email);
        }

        public async Task CreateTable(string tablePrefix)
        {
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = GetCreateSubscribersSql(tablePrefix);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<PagedResponse<SubscriberListResponse>> GetSubscribers(Team team, int? page, int? pageSize)
        {
            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;
            string sql = "SELECT * FROM " + team.TablePrefix + "_subscribers LIMIT " + skip + "," + take;
            string countSql = "SELECT COUNT(Id) FROM " + team.TablePrefix + "_subscribers";

            SetTableForTeam(team);
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var records = await connection.QueryAsync<Subscriber>(sql);
            int totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            return PagedResponseHelper<SubscriberListResponse>.CreateResponse(page, pageSize, totalCount, records.Select(s => MapListResponseFromSubscriber(s)));
        }

        public async Task<Subscriber> GetSubscriberByEmail(Team team, string email)
        {
            string sql = "SELECT * FROM " + team.TablePrefix + "_subscribers" + " WHERE Email = @Email";
            SetTableForTeam(team);
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Subscriber>(sql, new { Email = email });
        }

        public async Task<SubscriberDetailResponse> GetSubscriberById(Team team, int id)
        {
            string sql = "SELECT * FROM " + team.TablePrefix + "_subscribers" + " WHERE Id = @Id";
            SetTableForTeam(team);

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var subscriber = await connection.QuerySingleOrDefaultAsync<Subscriber>(sql, new { Id = id });
            return MapDetailResponseFromSubscriber(subscriber);
        }

        public async Task<int> CreateSubscriber(Subscriber subscriber, Team team)
        {
            SetTableForTeam(team);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.InsertAsync<Subscriber>(subscriber);
        }

        public async Task<bool> UpdateSubscriber(Subscriber subscriber, Team team)
        {
            SetTableForTeam(team);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.UpdateAsync(subscriber);
        }

        public async Task<bool> DeleteSubscriber(Subscriber subscriber, Team team)
        {
            SetTableForTeam(team);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.DeleteAsync(subscriber);
        }

        public async Task<Team> GetTeamByIdAsync(int id)
        {
            string sql = "SELECT * FROM Teams WHERE Id = @Id";

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Team>(sql, new { Id = id });
        }

        private static SubscriberListResponse MapListResponseFromSubscriber(Subscriber subscriber)
        {
            return new SubscriberListResponse(subscriber.Id,
                subscriber.TeamId,
                subscriber.Email,
                subscriber.FirstName,
                subscriber.LastName,
                subscriber.Subscribed,
                subscriber.LastAccessedAt,
                subscriber.SubscribedAt);
        }

        private static SubscriberDetailResponse MapDetailResponseFromSubscriber(Subscriber subscriber)
        {
            return new SubscriberDetailResponse(
                subscriber.Id,
                subscriber.TeamId,
                subscriber.Email,
                subscriber.FirstName,
                subscriber.LastName,
                subscriber.Subscribed,
                subscriber.LastAccessedAt,
                subscriber.SubscribedAt,
                subscriber.CreatedUserName,
                subscriber.UpdatedUserName,
                subscriber.CreatedAt,
                subscriber.UpdatedAt);
        }

        private void SetTableForTeam(Team team)
        {
            if (_tableMapped) return;
            SqlMapperExtensions.TableNameMapper = entityType =>
            {
                if (entityType == typeof(Subscriber))
                {
                    return team.TablePrefix + "_subscribers";
                }
                throw new Exception($"Not supported entity type {entityType}");
            };
            _tableMapped = true;
        }

        private string GetCreateSubscribersSql(string tablePrefix)
        {
            return "CREATE TABLE `" + tablePrefix + "_subscribers`" + @"`
(
	`Id` INT NOT NULL AUTO_INCREMENT,
	`TeamId` INT NOT NULL,
	`Email` VARCHAR(255),
	`FirstName` VARCHAR(40),
	`LastName` VARCHAR(40),
    `PasswordHash` VARCHAR(255),
  `SessionToken` text NULL,
  `SessionTokenExpiry` datetime NULL,
  `Subscribed` BIT NULL,
  `SubscribedAt` datetime NULL,
  `LastAccessedAt` datetime NULL,
  `LoginAttempts` INT NULL,
    `CreatedUserName` text,
  `UpdatedUserName` text,
  `CreatedAt` datetime NOT NULL,
  `UpdatedAt` datetime NOT NULL,
  `DeletedAt` datetime NOT NULL,
  PRIMARY KEY (`Id`)
)";
        }
    }
}
