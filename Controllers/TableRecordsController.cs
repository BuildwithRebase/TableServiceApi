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

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableRecordsController : ControllerBase
    {
        private readonly TableServiceContext _context;

        public TableRecordsController(TableServiceContext context)
        {
            _context = context;
        }

        // GET: api/TableRecords/search
        [HttpGet]
        [Route("search")]
        public async Task<ActionResult<IEnumerable<TableRecord>>> GetTableRecords([FromQuery] TableRecordSearchViewModel searchViewModel)
        {
            return await _context.TableRecords.Where(tr => tr.TeamName == searchViewModel.TeamName && tr.TableName == searchViewModel.TableName).Skip(0).Take(10).ToListAsync();
        }

        // GET: api/TableRecords/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TableRecord>> GetTableRecord(int id)
        {
            var tableRecord = await _context.TableRecords.FindAsync(id);

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

            _context.Entry(tableRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
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
            _context.TableRecords.Add(tableRecord);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTableRecord", new { id = tableRecord.Id }, tableRecord);
        }

        // DELETE: api/TableRecords/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTableRecord(int id)
        {
            var tableRecord = await _context.TableRecords.FindAsync(id);
            if (tableRecord == null)
            {
                return NotFound();
            }

            _context.TableRecords.Remove(tableRecord);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TableRecordExists(int id)
        {
            return _context.TableRecords.Any(e => e.Id == id);
        }
    }
}
