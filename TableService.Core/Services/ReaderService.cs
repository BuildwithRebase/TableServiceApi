using Dapper;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using TableService.Core.Exceptions;
using TableService.Core.Extensions;
using TableService.Core.Models;
using TableServiceApi.ViewModels;

namespace TableService.Core.Services
{
    public class ReaderService : IReaderService
    {
        private bool _tableMapped = false;
        public async Task<PagedResponse<Dictionary<string, object>>> GetForms(int teamId, string tableName, int? page, int? pageSize, string select, string filter)
        {
            var team = await GetTeamByIdAsync(teamId);
            if (team == null)
            {
                throw new MyHttpException(400, "Unable to find team");
            }
            SetTableForTeam(team.TablePrefix, tableName);
            var table = await GetTableByNameAsync(teamId, tableName);
            if (table == null)
            {
                throw new MyHttpException(400, "Unable to find table");
            }
            if (table.TablePrivacyModel > 0)
            {
                throw new MyHttpException(401, "Unauthorised to look at table");
            }

            StringBuilder clauseQuery = new StringBuilder();
            var filterObject = table.CreateQueryFromTableAndFilter(filter, clauseQuery);

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;

            StringBuilder sql = new StringBuilder("SELECT ")
                    .Append(table.CreateSelectFields(select)).Append(" ")
                    .Append("FROM ").Append(team.TablePrefix).Append("_").Append(tableName).Append(" ")
                    .Append("WHERE 1=1 ")
                    .Append(clauseQuery)
                    .Append(" LIMIT ").Append(skip).Append(",").Append(take);

            StringBuilder countSql = new StringBuilder("SELECT ")
                    .Append(" Count(Id) FROM ").Append(team.TablePrefix).Append("_").Append(tableName).Append(" ")
                    .Append("WHERE 1=1 ")
                    .Append(clauseQuery);

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var records = await connection.QueryAsync<TableRecord>(sql.ToString(), (object)filterObject);
            int totalCount = await connection.ExecuteScalarAsync<int>(countSql.ToString(), (object)filterObject);

            return PagedResponseHelper<Dictionary<string, object>>.CreateResponse(page, pageSize, totalCount, records.Select(s => s.MapToData(table, select)));
        }

        public async Task<Dictionary<string, object>> GetForm(int teamId, string tableName, int id)
        {
            var team = await GetTeamByIdAsync(teamId);
            if (team == null)
            {
                throw new MyHttpException(400, "Unable to find team");
            }
            SetTableForTeam(team.TablePrefix, tableName);
            var table = await GetTableByNameAsync(teamId, tableName);
            if (table == null)
            {
                throw new MyHttpException(400, "Unable to find table");
            }
            if (table.TablePrivacyModel > 0)
            {
                throw new MyHttpException(401, "Unauthorised to look at table");
            }
            

            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var sql = @"SELECT * FROM " + team.TablePrefix + "_" + tableName + " WHERE Id = @Id";
            var tableRecord = await connection.QueryFirstOrDefaultAsync<TableRecord>(sql, new { Id = id });
            if (tableRecord == null)
            {
                throw new MyHttpException(400, "Unable to find record");
            }

            return tableRecord.MapToData(table);
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
    }
}
