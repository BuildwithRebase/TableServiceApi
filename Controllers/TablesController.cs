using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableService.Core.Utility;
using TableServiceApi.ViewModels;

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
        public async Task<IActionResult> PutTable(int id, Table table)
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

            _context.Entry(table).State = EntityState.Modified;

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

            return Ok(new MessageViewModel("Revoked"));
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

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTable", new { id = table.Id }, table);
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

            return Ok(new MessageViewModel("Revoked"));
        }

        private bool TableExists(int id)
        {
            return _context.Tables.Any(e => e.Id == id);
        }

    }
}
