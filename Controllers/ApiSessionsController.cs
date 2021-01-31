using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TableService.Core.Contexts;
using TableService.Core.Models;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ApiSessionsController : ControllerBase
    {
        private readonly TableServiceContext _context;

        public ApiSessionsController(TableServiceContext context)
        {
            _context = context;
        }

        // GET: api/ApiSessions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ApiSession>>> GetApiSessions()
        {
            return await _context.ApiSessions.ToListAsync();
        }

        // GET: api/ApiSessions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiSession>> GetApiSession(int id)
        {
            var apiSession = await _context.ApiSessions.FindAsync(id);

            if (apiSession == null)
            {
                return NotFound();
            }

            return apiSession;
        }

        // PUT: api/ApiSessions/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutApiSession(int id, ApiSession apiSession)
        {
            if (id != apiSession.Id)
            {
                return BadRequest();
            }

            _context.Entry(apiSession).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApiSessionExists(id))
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

        // POST: api/ApiSessions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ApiSession>> PostApiSession(ApiSession apiSession)
        {
            _context.ApiSessions.Add(apiSession);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetApiSession", new { id = apiSession.Id }, apiSession);
        }

        // DELETE: api/ApiSessions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApiSession(int id)
        {
            var apiSession = await _context.ApiSessions.FindAsync(id);
            if (apiSession == null)
            {
                return NotFound();
            }

            _context.ApiSessions.Remove(apiSession);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ApiSessionExists(int id)
        {
            return _context.ApiSessions.Any(e => e.Id == id);
        }
    }
}
