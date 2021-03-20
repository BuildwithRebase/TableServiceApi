using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableService.Core.Utility;
using TableService.Core.Types;
using TableServiceApi.ViewModels;
using System;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TablesController : ControllerBase
    {
        private readonly TableServiceContext _context;

        public TablesController(TableServiceContext context)
        {
            _context = context;
        }

        // GET: api/Tables
        [HttpGet]
        public async Task<ActionResult<PagedResponseViewModel>> GetTables([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;


            int totalCount = await _context.Tables.Where(tbl => tbl.TeamId == teamId).CountAsync();
            var data = await _context.Tables.Where(tbl => tbl.TeamId == teamId).Skip(skip).Take(take).ToListAsync();

            var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data, DynamicClassUtility.GetFieldDefinitions(typeof(Table)));

            return response;
        }

        // GET: api/Tables/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Table>> GetTable(int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            var table = await _context.Tables.Where(tbl => tbl.TeamId == teamId && tbl.Id == id).FirstOrDefaultAsync();

            if (table == null)
            {
                return NotFound();
            }

            return table;
        }

        // PUT: api/Tables/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTable(int id, UpdateTableRequestViewModel table)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;
            if (!apiSession.UserRoles.Contains("SuperAdmin") && teamId != table.TeamId)
            {
                return Unauthorized();
            }

            if (id != table.Id)
            {
                return BadRequest();
            }

            var existingTable = _context.Tables.Where(tbl => tbl.Id == id).SingleOrDefault();
            if (existingTable == null)
            {
                return NotFound();
            }    

            // update fields individually if changed
            if (table.FieldNames != null && !existingTable.FieldNames.Equals(table.FieldNames))
            {
                existingTable.FieldNames = table.FieldNames;
            }
            if (table.FieldTypes != null && !existingTable.FieldTypes.Equals(table.FieldTypes))
            {
                existingTable.FieldTypes = table.FieldTypes;
            }
            if (table.TableState != null && !existingTable.TableState.Equals(table.TableState))
            {
                existingTable.TableState = (TableStateType)table.TableState;
            }
            if (table.TablePrivacyModel != null && !existingTable.TablePrivacyModel.Equals(table.TablePrivacyModel))
            {
                existingTable.TablePrivacyModel = (TablePrivacyModelType)table.TablePrivacyModel;
            }
            if (table.TableViewMode != null && !existingTable.TableViewMode.Equals(table.TableViewMode))
            {
                existingTable.TableViewMode = (TableViewModeType)table.TableViewMode;
            }

            existingTable.UpdatedAt = DateTime.Now;
            existingTable.UpdatedUserName = apiSession.UserName;

            _context.Entry(existingTable).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TableExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new MessageViewModel("Updated"));
        }

        // POST: api/Tables
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Table>> PostTable(Table table)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;
            if (!apiSession.UserRoles.Contains("SuperAdmin") && teamId != table.TeamId)
            {
                return Unauthorized();
            }

            var tableName = table.TableLabel.Replace(" ", "").ToLower();

            // Handle conflicts
            var existingTable = _context.Tables.Where(tbl => tbl.TeamId == teamId && tbl.TableName == tableName).SingleOrDefault();
            if (existingTable != null && existingTable.TableState != TableStateType.TableDeleted)
            {
                return Conflict(); // don't allow the same table to be created twice
            } else if (existingTable != null && existingTable.TableState == TableStateType.TableDeleted)
            {
                // allow the existing table to be resurrected
                existingTable.TableState = TableStateType.TableEditing;
                existingTable.UpdatedUserName = apiSession.UserName;
                existingTable.UpdatedAt = DateTime.Now;

                _context.Entry(existingTable).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return CreatedAtAction("GetTable", new { id = existingTable.Id }, existingTable);
            }
            else
            {
                table.TableName = tableName;
                table.TableState = TableStateType.TableCreated;
                table.TablePrivacyModel = TablePrivacyModelType.Private;
                table.UpdatedAt = DateTime.Now;
                table.UpdatedUserName = apiSession.UserName;
                table.CreatedAt = DateTime.Now;
                table.CreatedUserName = apiSession.UserName;

                _context.Tables.Add(table);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetTable", new { id = table.Id }, table);
            }
        }

        // DELETE: api/Tables/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTable(int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            var table = await _context.Tables.FindAsync(id);
            if (table == null)
            {
                return NotFound();
            }

            if (!apiSession.UserRoles.Contains("SuperAdmin") && teamId != table.TeamId)
            {
                return Unauthorized();
            }

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();

            return Ok(new MessageViewModel("Deleted"));
        }

        private bool TableExists(int id)
        {
            return _context.Tables.Any(e => e.Id == id);
        }

    }
}
