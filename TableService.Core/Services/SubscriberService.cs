using System;
using System.Linq;
using System.Text;
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
using System.Text.Json;
using TableService.Core.Extensions;

namespace TableService.Core.Services
{
    public class SubscriberService : ISubscriberService
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

            Subscriber existingSubscriber = await GetSubscriberByEmail(team.TablePrefix, subscribeUserRequest.Email);
            if (existingSubscriber != null)
            {
                throw new MyHttpException(400, "Subscriber already exists with this email address");
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

            SetTableForTeam(team.TablePrefix);
            int id = await CreateSubscriber(subscriber, team.TablePrefix);

            return new SubscribeResponse(id, subscribeUserRequest.Email);
        }

        public async Task<bool> Unsubscribe(UnsubscribeUserRequest request, ApiSession apiSession)
        {
            SetTableForTeam(apiSession.TablePrefix);
            Subscriber subscriber = await GetSubscriberByEmail(apiSession.TablePrefix, request.Email);
            subscriber.SessionToken = "";
            subscriber.Subscribed = false;
            subscriber.UpdatedUserName = request.Email;
            subscriber.UpdatedAt = DateTime.Now;

            if (!await UpdateSubscriberInternal(subscriber, apiSession.TablePrefix))
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

            SetTableForTeam(team.TablePrefix);
            Subscriber subscriber = await GetSubscriberByEmail(team.TablePrefix, request.Email);
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

                await UpdateSubscriberInternal(subscriber, team.TablePrefix);

                throw new MyHttpException(401, "Invalid Email or Password");
            }

            subscriber.SessionToken = GuidHelper.CreateCryptographicallySecureGuid().ToString();
            subscriber.SessionTokenExpiry = DateTime.Now.AddMinutes(30);
            subscriber.LastAccessedAt = DateTime.Now;
            subscriber.Locked = false;
            subscriber.LoginAttempts = 0;

            await UpdateSubscriberInternal(subscriber, team.TablePrefix);

            var claimsIdentity = ClaimsIdentityFactory.ClaimsIdentityFromSubscriber(subscriber);
            var jwt = JwtUtility.GenerateToken(claimsIdentity);

            return new LoginResponse(jwt, subscriber.Id, subscriber.Email, subscriber.FirstName, subscriber.LastName, subscriber.TeamId, team.TeamName, new List<TeamTable>());
        }

        public async Task<bool> Logout(ApiSession apiSession)
        {
            SetTableForTeam(apiSession.TablePrefix);
            Subscriber subscriber = await GetSubscriberByEmail(apiSession.TablePrefix, apiSession.Email);
            if (subscriber == null)
            {
                throw new MyHttpException(400, "Unable to find subscriber");
            }

            subscriber.SessionToken = "";
            await UpdateSubscriberInternal(subscriber, apiSession.TablePrefix);

            return true;
        }

        public async Task<ChangePasswordResponse> ChangePassword(SubscriberChangePasswordRequest request, ApiSession apiSession)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.CurrentPassword) ||
                string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.NewPasswordConfirm))
            {
                throw new MyHttpException(400, "Email, CurrentPassword, NewPassword and NewPasswordConfirm are required");
            }
            //Team team = await GetTeamByIdAsync(request.TeamId);
            //if (team == null)
            //{
            //    throw new MyHttpException(400, "Unable to find team");
            //}
            if (!request.NewPassword.Equals(request.NewPassword))
            {
                throw new MyHttpException(400, "Passwords do not match");
            }

            SetTableForTeam(apiSession.TablePrefix);
            Subscriber subscriber = await GetSubscriberByEmail(apiSession.TablePrefix, request.Email);
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

            await UpdateSubscriberInternal(subscriber, apiSession.TablePrefix);

            return new ChangePasswordResponse(subscriber.Id, apiSession.TeamId, subscriber.Email);
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

        public async Task<PagedResponse<SubscriberListResponse>> GetSubscribers(string tablePrefix, int? page, int? pageSize)
        {
            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;
            string sql = "SELECT * FROM " + tablePrefix + "_subscribers LIMIT " + skip + "," + take;
            string countSql = "SELECT COUNT(Id) FROM " + tablePrefix + "_subscribers";

            SetTableForTeam(tablePrefix);
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var records = await connection.QueryAsync<Subscriber>(sql);
            int totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            return PagedResponseHelper<SubscriberListResponse>.CreateResponse(page, pageSize, totalCount, records.Select(s => MapListResponseFromSubscriber(s)));
        }

        public async Task<Subscriber> GetSubscriberByEmail(string tablePrefix, string email)
        {
            string sql = "SELECT * FROM " + tablePrefix + "_subscribers" + " WHERE Email = @Email";
            SetTableForTeam(tablePrefix);
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Subscriber>(sql, new { Email = email });
        }

        public async Task<SubscriberDetailResponse> GetSubscriberById(ApiSession apiSession, int id)
        {
            string sql = "SELECT * FROM " + apiSession.TablePrefix + "_subscribers" + " WHERE Id = @Id";
            SetTableForTeam(apiSession.TablePrefix);

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var subscriber = await connection.QuerySingleOrDefaultAsync<Subscriber>(sql, new { Id = id });
            return MapDetailResponseFromSubscriber(subscriber);
        }

        public async Task<int> CreateSubscriber(Subscriber subscriber, string tablePrefix)
        {
            SetTableForTeam(tablePrefix);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.InsertAsync<Subscriber>(subscriber);
        }

        public async Task<int> CreateSubscriber(ApiSession apiSession, SubscriberAddRequest request)
        {
            if (request.Password != request.ConfirmPassword)
            {
                throw new MyHttpException(400, "Passwords do not match");
            }

            Subscriber existingSubscriber = await GetSubscriberByEmail(apiSession.TablePrefix, request.Email);
            if (existingSubscriber != null)
            {
                throw new MyHttpException(400, "Subscriber already exists with this email address");
            }

            SetTableForTeam(apiSession.TablePrefix);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            Subscriber subscriber = new Subscriber();
            subscriber.Email = request.Email;
            subscriber.FirstName = request.FirstName;
            subscriber.LastName = request.LastName;
            subscriber.Subscribed = true;
            subscriber.SubscribedAt = DateTime.Now;
            subscriber.PasswordHash = PasswordUtility.HashPassword(request.Password);
            subscriber.UpdatedAt = DateTime.Now;
            subscriber.UpdatedUserName = apiSession.Email;
            
            return await connection.InsertAsync(subscriber);
        }

        public async Task<bool> UpdateSubscriber(ApiSession apiSession, int id, SubscriberUpdateRequest request)
        {
            SetTableForTeam(apiSession.TablePrefix);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            Subscriber subscriber = await connection.GetAsync<Subscriber>(id);
            if (subscriber == null)
            {
                throw new MyHttpException(400, "Unable to find subscriber");
            }

            subscriber.Email = string.IsNullOrEmpty(request.Email) ? subscriber.Email : request.Email;
            subscriber.FirstName = string.IsNullOrEmpty(request.FirstName) ? subscriber.FirstName : request.FirstName;
            subscriber.LastName = string.IsNullOrEmpty(request.LastName) ? subscriber.FirstName : request.LastName;
            subscriber.Subscribed = request.Subscribed;
            subscriber.UpdatedAt = DateTime.Now;
            subscriber.UpdatedUserName = apiSession.Email;

            return await connection.UpdateAsync(subscriber);
        }

        private async Task<bool> UpdateSubscriberInternal(Subscriber subscriber, string tablePrefix)
        {
            SetTableForTeam(tablePrefix);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.UpdateAsync(subscriber);
        }

        public async Task<bool> ResetSubscriberPassword(ApiSession apiSession, int id, SubscriberResetPasswordRequest request)
        {
            if (request.Password != request.ConfirmPassword)
            {
                throw new MyHttpException(400, "Passwords do not match");
            }

            SetTableForTeam(apiSession.TablePrefix);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            Subscriber subscriber = await connection.GetAsync<Subscriber>(id);
            if (subscriber == null)
            {
                throw new MyHttpException(400, "Unable to find subscriber");
            }

            subscriber.PasswordHash = PasswordUtility.HashPassword(request.Password);
            subscriber.UpdatedAt = DateTime.Now;
            subscriber.UpdatedUserName = apiSession.Email;

            return await connection.UpdateAsync(subscriber);
        }

        public async Task<bool> DeleteSubscriber(int id, string tablePrefix)
        {
            SetTableForTeam(tablePrefix);
            var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            Subscriber subscriber = await connection.GetAsync<Subscriber>(id);
            if (subscriber == null)
            {
                throw new MyHttpException(400, "Unable to find subscriber");
            }

            return await connection.DeleteAsync(subscriber);
        }

        public async Task<Team> GetTeamByIdAsync(int id)
        {
            string sql = "SELECT * FROM Teams WHERE Id = @Id";

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Team>(sql, new { Id = id });
        }

        private async Task<Table> GetTableByNameAsync(int teamId, string tableName)
        {
            string sql = "SELECT * FROM Tables WHERE TeamId = @TeamId AND TableName = @TableName";

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Table>(sql, new { TeamId = teamId, TableName = tableName });
        }

        public async Task<int> SubmitForm(ApiSession apiSession, string tableName, Dictionary<string, object> data)
        {
            SetTableForTeam(apiSession.TablePrefix, tableName);
            var table = await GetTableByNameAsync(apiSession.TeamId, tableName);
            if (table == null)
            {
                throw new MyHttpException(400, "Unable to find table");
            }
            if (table.TablePrivacyModel > 1)
            {
                throw new MyHttpException(401, "Unauthorised to access table");
            }
            var subscriber = await GetSubscriberByEmail(apiSession.TablePrefix, apiSession.Email);

            var tableRecord = new TableRecord
            {
                TeamId = apiSession.TeamId,
                TeamName = apiSession.TeamName,
                TableName = tableName,
                SubscriberId = subscriber?.Id ?? 0,
                CreatedAt = DateTime.Now,
                CreatedUserName = apiSession.Email,
                UpdatedAt = DateTime.Now,
                UpdatedUserName = apiSession.Email
            };
            
            tableRecord.PopulateWithData(table, data);

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.InsertAsync(tableRecord);
        }

        public async Task<PagedResponse<Dictionary<string, object>>> GetForms(ApiSession apiSession, string tableName, int? page, int? pageSize, string select, string filter)
        {
            SetTableForTeam(apiSession.TablePrefix, tableName);
            var table = await GetTableByNameAsync(apiSession.TeamId, tableName);
            if (table == null)
            {
                throw new MyHttpException(400, "Unable to find table");
            }
            if (table.TablePrivacyModel > 1)
            {
                throw new MyHttpException(401, "Unauthorised to access table");
            }
            var subscriber = await GetSubscriberByEmail(apiSession.TablePrefix, apiSession.Email);
            if (subscriber == null)
            {
                throw new MyHttpException(400, "Unable to find subscriber");
            }

            // &filter=TaskName eq "*This* and TaskStatus eq "Open"
            StringBuilder clauseQuery = new StringBuilder();
            var filterObject = table.CreateQueryFromTableAndFilter(filter, clauseQuery);

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;

            StringBuilder sql = new StringBuilder("SELECT ")
                    .Append(table.CreateSelectFields(select)).Append(" ")
                    .Append("FROM ").Append(apiSession.TablePrefix).Append("_").Append(tableName).Append(" ")
                    .Append("WHERE SubscriberId = @SubscriberId ")
                    .Append(clauseQuery)
                    .Append(" LIMIT ").Append(skip).Append(",").Append(take);

            StringBuilder countSql = new StringBuilder("SELECT ")
                    .Append(" Count(Id) FROM ").Append(apiSession.TablePrefix).Append("_").Append(tableName).Append(" ")
                    .Append("WHERE SubscriberId = @SubscriberId ")
                    .Append(clauseQuery);

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            filterObject["SubscriberId"] = subscriber.Id;

            var records = await connection.QueryAsync<TableRecord>(sql.ToString(), (object)filterObject);
            int totalCount = await connection.ExecuteScalarAsync<int>(countSql.ToString(), (object)filterObject);

            return PagedResponseHelper<Dictionary<string, object>>.CreateResponse(page, pageSize, totalCount, records.Select(s => s.MapToData(table, select)));
        }

        public async Task<Dictionary<string, object>> GetForm(ApiSession apiSession, string tableName, int id)
        {
            SetTableForTeam(apiSession.TablePrefix, tableName);
            var table = await GetTableByNameAsync(apiSession.TeamId, tableName);
            if (table == null)
            {
                throw new MyHttpException(400, "Unable to find table");
            }
            if (table.TablePrivacyModel > 1)
            {
                throw new MyHttpException(401, "Unauthorised to access table");
            }
            var subscriber = await GetSubscriberByEmail(apiSession.TablePrefix, apiSession.Email);
            if (subscriber == null)
            {
                throw new MyHttpException(400, "Unable to find subscriber");
            }
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var sql = @"SELECT * FROM " + apiSession.TablePrefix + "_" + tableName + " WHERE Id = @Id";
            var tableRecord = await connection.QueryFirstOrDefaultAsync<TableRecord>(sql, new { Id = id } );
            if (tableRecord == null)
            {
                throw new MyHttpException(400, "Unable to find record");
            }
            if (tableRecord.SubscriberId != subscriber.Id)
            {
                throw new MyHttpException(401, "Not authorized to look at this record");
            }

            return tableRecord.MapToData(table);
        }

        public static SubscriberListResponse MapListResponseFromSubscriber(Subscriber subscriber)
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

        public static SubscriberDetailResponse MapDetailResponseFromSubscriber(Subscriber subscriber)
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

        private void SetTableForTeam(string tablePrefix, string tableName = null)
        {
            if (_tableMapped) return;
            SqlMapperExtensions.TableNameMapper = entityType =>
            {
                if (entityType == typeof(Subscriber))
                {
                    return tablePrefix + "_subscribers";
                }
                if (!string.IsNullOrEmpty(tableName) && entityType == typeof(TableRecord))
                {
                    return tablePrefix + "_" + tableName;
                }
                throw new Exception($"Not supported entity type {entityType}");
            };
            _tableMapped = true;
        }

        private string GetCreateSubscribersSql(string tablePrefix)
        {
            return "CREATE TABLE `" + tablePrefix + "_subscribers`" + @"
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
  `Locked` BIT NULL,
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
