using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableService.Core.Utility;
using TableServiceApi.TableService.Core.Contexts;
using TableServiceApi.ViewModels;
using Dapper;
using MySql.Data.MySqlClient;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly TableServiceContext _context;

        public DataController(TableServiceContext context, TeamDbContext teamDbContext)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the available tables and definitions
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var tables = await _context.Tables.Where(tbl => tbl.TeamId == apiSession.TeamId).ToListAsync();
            
            if (tables.Count == 0)
            {
                return NotFound("No tables defined for team");
            }

            var results = new Dictionary<string, object>();
            foreach (var table in tables)
            {

                var fields = table.ToFieldDefinitions();
                var type = DynamicClassUtility.CreateType(Char.ToUpperInvariant(table.TableName[0]) + table.TableName.Substring(1), fields);

                object record = Activator.CreateInstance(type);
                foreach (var field in fields)
                {
                    if (field.FieldType == "datetime")
                    {
                        DynamicClassUtility.SetFieldValue(type, record, field.FieldName, DateTime.Now);
                    }
                    else if (field.FieldType == "number")
                    {
                        DynamicClassUtility.SetFieldValue(type, record, field.FieldName, 0);
                    }
                    else
                    {
                        DynamicClassUtility.SetFieldValue(type, record, field.FieldName, field.FieldType);
                    }
                }

                results.Add(table.TableName, record);
            }

            return Ok(JsonConvert.SerializeObject(results, Formatting.Indented));
        }

        [HttpGet]
        [Route("{tableName}")]
        public async Task<ActionResult> Get([FromRoute] string tableName, [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var table = GetTableByName(apiSession.TeamId, tableName);

            if (table == null)
            {
               return NotFound("Table: " + tableName + " not found");
            }

            var fields = table.ToFieldDefinitions();
            var objectType = DynamicClassUtility.CreateType(Char.ToUpperInvariant(tableName[0]) + tableName.Substring(1), fields);

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;

//            var team = await _context.Teams.Where(t => t.Id == apiSession.TeamId).SingleOrDefaultAsync();

            string sql = "SELECT * FROM " + apiSession.TablePrefix + "_" + table.TableName + " LIMIT " + skip + "," + take;
            string countSql = "SELECT COUNT(Id) FROM " + apiSession.TablePrefix + "_" + table.TableName;

            using (var connection = new MySqlConnection(TableServiceContext.ConnectionString))
            {
                var records = await connection.QueryAsync<TableRecord>(sql);
                var data = records.Select(record => TableUtility.MapTableRecordToObject(tableName, record, objectType, fields));

                int totalCount = await connection.ExecuteScalarAsync<int>(countSql);
                var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data.ToList(), fields);

                return Ok(JsonConvert.SerializeObject(response, Formatting.Indented));
            }
        }

        [HttpGet]
        [Route("{tableName}/{id}")]
        public async Task<ActionResult> Get([FromRoute] string tableName, [FromRoute] int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var table = GetTableByName(apiSession.TeamId, tableName);

            if (table == null)
            {
                return NotFound("Table: " + tableName + " not found");
            }

            var fields = table.ToFieldDefinitions();
            var objectType = DynamicClassUtility.CreateType(Char.ToUpperInvariant(tableName[0]) + tableName.Substring(1), fields);

//            var team = await _context.Teams.Where(t => t.Id == apiSession.TeamId).SingleOrDefaultAsync();

            string sql = "SELECT * FROM " + apiSession.TablePrefix + "_" + table.TableName + " WHERE Id = @Id";
            using (var connection = new MySqlConnection(TableServiceContext.ConnectionString))
            {
                var data = await connection.QuerySingleOrDefaultAsync<TableRecord>(sql, new { Id = id });
                if (data == null)
                {
                    return NotFound();
                }
                var record = TableUtility.MapTableRecordToObject(tableName, data, objectType, fields);
                return Ok(JsonConvert.SerializeObject(record, Formatting.Indented));
            }
        }


        // POST: api/Tables
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("{tableName}")]
        public async Task<ActionResult> PostData([FromRoute] string tableName, [FromBody] Dictionary<string, object> data)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var table = GetTableByName(apiSession.TeamId, tableName);

            if (table == null)
            {
                return NotFound("Table: " + tableName + " not found");
            }

            var tableRecord = TableUtility.CreateTableRecordFromTable(apiSession, table, data, false, null);

//            var team = await _context.Teams.Where(t => t.Id == apiSession.TeamId).SingleOrDefaultAsync();
            var sql = GetInsertSql(apiSession.TeamId, apiSession.TeamName, apiSession.TablePrefix, table.TableName);
            using (var connection = new MySqlConnection(TableServiceContext.ConnectionString))
            {
                connection.Open();
                await connection.ExecuteAsync(sql, tableRecord);
                return Ok(new MessageViewModel("Data record created"));
            }
        }

        [HttpPost("{tableName}/{id}/update")]
        public async Task<IActionResult> UpdateData([FromRoute] string tableName, [FromRoute] int id, [FromBody] Dictionary<string, object> data)
        {
            return await this.PutData(tableName, id, data);
        }

        // PUT: api/Data/Task/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{tableName}/{id}")]
        public async Task<IActionResult> PutData([FromRoute] string tableName, [FromRoute] int id, [FromBody] Dictionary<string, object> data)
        {
            if (!data.ContainsKey("Id"))
            {
                return BadRequest();
            }

            int dataId = ((JsonElement)data["Id"]).GetInt32();

            if (id != (int) dataId)
            {
                return BadRequest();
            }

            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var table = GetTableByName(apiSession.TeamId, tableName);

            if (table == null)
            {
                return NotFound("Table: " + tableName + " not found");
            }
//            var team = await _context.Teams.Where(t => t.Id == apiSession.TeamId).SingleOrDefaultAsync();

            using (var connection = new MySqlConnection(TableServiceContext.ConnectionString))
            {
                connection.Open();


                var oldRecord = await connection.QuerySingleOrDefaultAsync<TableRecord>("SELECT * FROM " + apiSession.TablePrefix + "_" + table.TableName + " WHERE Id = @Id", new { Id = id });
                if (oldRecord == null)
                {
                    return NotFound();
                }

                var tableRecord = TableUtility.CreateTableRecordFromTable(apiSession, table, data, true, oldRecord);

                var sql = GetUpdateSql(apiSession.TablePrefix, table.TableName);
                connection.Execute(sql, tableRecord);
                return Ok(new MessageViewModel("Data record updated"));
            }
        }

        [HttpPost("{tableName}/{id}/delete")]
        public async Task<IActionResult> PostDeleteTable([FromRoute] string tableName, [FromRoute] int id)
        {
            return await this.DeleteTable(tableName, id);             
        }

        // DELETE: api/Data/Task/5
        [HttpDelete("{tableName}/{id}")]
        public async Task<IActionResult> DeleteTable([FromRoute] string tableName, [FromRoute] int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var table = GetTableByName(apiSession.TeamId, tableName);

            if (table == null)
            {
                return NotFound("Table: " + tableName + " not found");
            }

//            var team = await _context.Teams.Where(t => t.Id == apiSession.TeamId).SingleOrDefaultAsync();
            using (var connection = new MySqlConnection(TableServiceContext.ConnectionString))
            {
                connection.Open();

                var oldRecord = await connection.QuerySingleOrDefaultAsync<TableRecord>("SELECT * FROM " + apiSession.TablePrefix + "_" + table.TableName + " WHERE Id = @Id", new { Id = id });
                if (oldRecord == null)
                {
                    return NotFound();
                }

                var sql = "DELETE FROM " + apiSession.TablePrefix + "_" + table.TableName + " WHERE Id = @Id";
                connection.Execute(sql, new { Id = id });
                return Ok(new MessageViewModel("Data record deleted"));
            }
        }

        private Table GetTableByName(int teamId, string tableName)
        {
            return _context.Tables.Where(tbl => tbl.TeamId == teamId && tbl.TableName.ToLower() == tableName.ToLower()).FirstOrDefault();
        }

        private string GetInsertSql(int teamId, string teamName, string tablePrefix, string tableName)
        {
            return "INSERT INTO " + tablePrefix + "_" + tableName + @"
            (TeamId, TeamName, TableName, Field1StringValue, Field1NumberValue, Field1DateTimeValue, 
                Field2StringValue, Field2NumberValue, Field2DateTimeValue, 
                Field3StringValue, Field3NumberValue, Field3DateTimeValue, 
                Field4StringValue, Field4NumberValue, Field4DateTimeValue, 
                Field5StringValue, Field5NumberValue, Field5DateTimeValue, 
            CreatedUserName, UpdatedUserName, CreatedAt, UpdatedAt)
            VALUES(" + teamId + @", '" + teamName + @"', '" + tableName + @"', 
                @Field1StringValue, @Field1NumberValue, @Field1DateTimeValue, 
                @Field2StringValue, @Field2NumberValue, @Field2DateTimeValue,
                @Field3StringValue, @Field3NumberValue, @Field3DateTimeValue,
                @Field4StringValue, @Field4NumberValue, @Field4DateTimeValue,
                @Field5StringValue, @Field5NumberValue, @Field5DateTimeValue,
                @CreatedUserName, @UpdatedUserName, @CreatedAt, @UpdatedAt)";
        }

        private string GetUpdateSql(string tablePrefix, string tableName)
        {
            return "UPDATE " + tablePrefix + "_" + tableName + @"
                SET Field1StringValue = @Field1StringValue, Field1NumberValue = @Field1NumberValue, Field1DateTimeValue = @Field1DateTimeValue, 
                Field2StringValue = @Field2StringValue, Field2NumberValue = @Field2NumberValue, Field2DateTimeValue = @Field2DateTimeValue, 
                Field3StringValue = @Field3StringValue, Field3NumberValue = @Field3NumberValue, Field3DateTimeValue = @Field3DateTimeValue, 
                Field4StringValue = @Field4StringValue, Field4NumberValue = @Field4NumberValue, Field4DateTimeValue = @Field4DateTimeValue, 
                Field5StringValue = @Field5StringValue, Field5NumberValue = @Field5NumberValue, Field5DateTimeValue = @Field5DateTimeValue, 
                UpdatedUserName = '', UpdatedAt = ''
                WHERE Id = @Id;";
        }
    }
}
