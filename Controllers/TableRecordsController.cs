using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableServiceApi.ViewModels;
using System.Reflection;
using TableServiceApi.TableService.Core.Contexts;
using Microsoft.AspNetCore.Authorization;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class TableRecordsController : ControllerBase
    {
        private readonly TableServiceContext _context;
        private readonly TeamDbContext _teamDbContext;

        public TableRecordsController(TableServiceContext context, TeamDbContext teamDbContext)
        {
            _context = context;
            _teamDbContext = teamDbContext;
        }

        // GET: api/TableRecords
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TableRecord>>> GetTables()
        {
            _teamDbContext.Database.EnsureCreated();

            return await _teamDbContext.TableRecords.ToListAsync();
        }

        // GET: api/TableRecords/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TableRecord>> GetTableRecord(int id)
        {
            _teamDbContext.Database.EnsureCreated();
            
            var tableRecord = await _teamDbContext.TableRecords.FindAsync(id);

            if (tableRecord == null)
            {
                return NotFound();
            }

            return tableRecord;
        }

        // PUT: api/TableRecords/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTableRecord(int id, TableRecord tableRecord)
        {
            if (id != tableRecord.Id)
            {
                return BadRequest();
            }

            _teamDbContext.Database.EnsureCreated();
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

            return NoContent();
        }

        // POST: api/TableRecords
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TableRecord>> PostTableRecord(TableRecord tableRecord)
        {
            _teamDbContext.Database.EnsureCreated();
            _teamDbContext.TableRecords.Add(tableRecord);
            await _teamDbContext.SaveChangesAsync();

            return CreatedAtAction("GetTableRecord", new { id = tableRecord.Id }, tableRecord);
        }

        // DELETE: api/TableRecords/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTableRecord(int id)
        {
            _teamDbContext.Database.EnsureCreated();
            var tableRecord = await _teamDbContext.TableRecords.FindAsync(id);
            if (tableRecord == null)
            {
                return NotFound();
            }

            _teamDbContext.TableRecords.Remove(tableRecord);
            await _teamDbContext.SaveChangesAsync();

            return NoContent();
        }

        private bool TableRecordExists(int id)
        {
            return _teamDbContext.TableRecords.Any(e => e.Id == id);
        }

    }
}
