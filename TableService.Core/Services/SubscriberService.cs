using System;
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

namespace TableService.Core.Services
{
    public class SubscriberService
    {
        public async Task<Subscriber> Subscribe(SubscribeUserRequest subscribeUserRequest)
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

            int id = await CreateSubscriber(subscriber);
            return await GetSubscriberById(team, id);
        }

        public async Task<Subscriber> Unsubscribe(UnsubscribeUserRequest request)
        {
            Team team = await GetTeamByIdAsync(request.TeamId);
            if (team == null)
            {
                throw new MyHttpException(400, "Unable to find team");
            }

            Subscriber subscriber = await GetSubscriberById(team, request.Email);
            subscriber.Subscribed = false;
            subscriber.UpdatedUserName = request.Email;
            subscriber.UpdatedAt = DateTime.Now;

            if (!await UpdateSubscriber(subscriber))
            {
                throw new MyHttpException(500, "Unable to update subscriber record");
            }

            return subscriber;
        }

        public async Task<Subscriber> Login(SubscriberLoginRequest request)
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

            Subscriber subscriber = await GetSubscriberById(team, request.Email);
            if (!PasswordUtility.VerifyPassword(request.Password, subscriber.PasswordHash))
            {
                throw new MyHttpException(401, "Invalid Email or Password");
            }
            return subscriber;
        }

        public async Task<Subscriber> ChangePassword(SubscriberChangePasswordRequest request)
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

            Subscriber subscriber = await GetSubscriberById(team, request.Email);
            if (!PasswordUtility.VerifyPassword(request.CurrentPassword, subscriber.PasswordHash))
            {
                throw new MyHttpException(401, "Invalid Email or CurrentPassword");
            }

            subscriber.PasswordHash = PasswordUtility.HashPassword(request.NewPassword);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            await connection.UpdateAsync<Subscriber>(subscriber);
            return subscriber;
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

        public async Task<PagedResponse<Subscriber>> GetSubscribers(Team team, int? page, int? pageSize)
        {
            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;
            string sql = "SELECT * FROM " + team.TablePrefix + "_subscribers LIMIT " + skip + "," + take;
            string countSql = "SELECT COUNT(Id) FROM " + team.TablePrefix + "_subscribers";

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var records = await connection.QueryAsync<Subscriber>(sql);
            int totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            return PagedResponseHelper<Subscriber>.CreateResponse(page, pageSize, totalCount, records);
        }

        public async Task<Subscriber> GetSubscriberById(Team team, string email)
        {
            string sql = "SELECT * FROM " + team.TablePrefix + "_subscribers" + " WHERE Email = @Email";

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Subscriber>(sql, new { Email = email });
        }

        public async Task<Subscriber> GetSubscriberById(Team team, int id)
        {
            string sql = "SELECT * FROM " + team.TablePrefix + "_subscribers" + " WHERE Id = @Id";

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Subscriber>(sql, new { Id = id });
        }

        public async Task<int> CreateSubscriber(Subscriber subscriber)
        {
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.InsertAsync<Subscriber>(subscriber);
        }

        public async Task<bool> UpdateSubscriber(Subscriber subscriber)
        {
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.UpdateAsync(subscriber);
        }

        public async Task<bool> DeleteSubscriber(Subscriber subscriber, Team team)
        {
            var sql = @"DELETE FROM " + team.TablePrefix + "_subscribers WHERE Id = @Id";

            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.DeleteAsync(subscriber);
        }

        private async Task<Team> GetTeamByIdAsync(int id)
        {
            string sql = "SELECT * FROM Teams WHERE Id = @Id";

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Team>(sql, new { Id = id });
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
  `SubscribedFg` BIT NULL,
  `SubscribedAt` datetime NULL,
  `LastAccessedAt` datetime NULL,
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
