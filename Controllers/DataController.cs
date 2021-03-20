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

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly TableServiceContext _context;
        private readonly TeamDbContext _teamDbContext;

        public DataController(TableServiceContext context, TeamDbContext teamDbContext)
        {
            _context = context;
            _teamDbContext = teamDbContext;
        }


        /// <summary>
        /// Gets the available tables and definitions
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            _teamDbContext.Database.EnsureCreated();

            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var tables = _context.Tables.Where(tbl => tbl.TeamId == apiSession.TeamId).ToList();
            
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
            _teamDbContext.Database.EnsureCreated();

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

            var data = _teamDbContext.TableRecords
                .Where(tbl => tbl.TableName.ToLower() == tableName.ToLower())
                .Skip(skip).Take(take)
                .Select(record => TableUtility.MapTableRecordToObject(tableName, record, objectType, fields));

            int totalCount = _teamDbContext.TableRecords.Where(tbl => tbl.TableName.ToLower() == tableName.ToLower()).Count();
            var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data.ToList(), fields);

            return Ok(JsonConvert.SerializeObject(response, Formatting.Indented));
        }

        [HttpGet]
        [Route("{tableName}/{id}")]
        public async Task<ActionResult> Get([FromRoute] string tableName, [FromRoute] int id)
        {
            _teamDbContext.Database.EnsureCreated();

            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var table = GetTableByName(apiSession.TeamId, tableName);

            if (table == null)
            {
                return NotFound("Table: " + tableName + " not found");
            }

            var fields = table.ToFieldDefinitions();
            var objectType = DynamicClassUtility.CreateType(Char.ToUpperInvariant(tableName[0]) + tableName.Substring(1), fields);

            var data = _teamDbContext.TableRecords
                .Where(tbl => tbl.Id == id)
                .Select(record => TableUtility.MapTableRecordToObject(tableName, record, objectType, fields))
                .FirstOrDefault();
            if (data == null)
            {
                return NotFound();
            }    

            return Ok(JsonConvert.SerializeObject(data, Formatting.Indented));
        }


        // POST: api/Tables
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("{tableName}")]
        public async Task<ActionResult> PostData([FromRoute] string tableName, [FromBody] Dictionary<string, object> data)
        {
            _teamDbContext.Database.EnsureCreated();

            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var table = GetTableByName(apiSession.TeamId, tableName);

            if (table == null)
            {
                return NotFound("Table: " + tableName + " not found");
            }

            var tableRecord = TableUtility.CreateTableRecordFromTable(apiSession, table, data, false, null);

            _teamDbContext.Add(tableRecord);
            await _teamDbContext.SaveChangesAsync();

            return Ok(new MessageViewModel("Data record created"));
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

            var oldRecord = _teamDbContext.TableRecords
                                .Where(tbl => tbl.Id == id)
                                .FirstOrDefault();
            if (oldRecord == null)
            {
                return NotFound();
            }

            var tableRecord = TableUtility.CreateTableRecordFromTable(apiSession, table, data, true, oldRecord);

            _teamDbContext.Entry(tableRecord).State = EntityState.Modified;

            try
            {
                await _teamDbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TableRecordExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new MessageViewModel("Table updated"));
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


            var tableRecord = await _teamDbContext.TableRecords.FindAsync(id);
            if (tableRecord == null)
            {
                return NotFound();
            }

            _teamDbContext.TableRecords.Remove(tableRecord);
            await _teamDbContext.SaveChangesAsync();

            return Ok(new MessageViewModel("Table row deleted"));
        }

        private Table GetTableByName(int teamId, string tableName)
        {
            return _context.Tables.Where(tbl => tbl.TeamId == teamId && tbl.TableName.ToLower() == tableName.ToLower()).FirstOrDefault();
        }

        private bool TableRecordExists(int id)
        {
            return _teamDbContext.TableRecords.Any(e => e.Id == id);
        }
    }
}
