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
using TableServiceApi.Filters;
using TableServiceApi.ViewModels;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    //[MyAuthorization("AdminUser")]
    public class TeamsController : ControllerBase
    {
        private readonly TableServiceContext _context;

        public TeamsController(TableServiceContext context)
        {
            _context = context;
        }

        // GET: api/Teams
        [HttpGet]
        public async Task<ActionResult<PagedResponseViewModel>> GetTeams([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }
            if (!apiSession.UserRoles.Contains("SuperAdmin"))
            {
                return Unauthorized("Only Super Admins can search through all teams (for now:))");
            }

            var teamId = apiSession.TeamId;

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;


            int totalCount = await _context.Teams.CountAsync();
            var data = await _context.Teams.Skip(skip).Take(take).ToListAsync();

            var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data, DynamicClassUtility.GetFieldDefinitions(typeof(Team)));

            return response;
        }

        // GET: api/Teams/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Team>> GetTeam(int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }
            if (!apiSession.UserRoles.Contains("SuperAdmin") && apiSession.TeamId != id)
            {
                return Unauthorized();
            }

            var team = await _context.Teams.FindAsync(id);

            if (team == null)
            {
                return NotFound();
            }

            return team;
        }

        // PUT: api/Teams/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeam(int id, Team team)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }
            if (!apiSession.UserRoles.Contains("SuperAdmin") && apiSession.TeamId != id)
            {
                return Unauthorized();
            }

            if (id != team.Id)
            {
                return BadRequest();
            }

            _context.Entry(team).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeamExists(id))
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

        // POST: api/Teams
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Team>> PostTeam(Team team)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }
            if (!apiSession.UserRoles.Contains("SuperAdmin"))
            {
                return Unauthorized();
            }

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTeam", new { id = team.Id }, team);
        }

        // DELETE: api/Teams/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }
            if (!apiSession.UserRoles.Contains("SuperAdmin"))
            {
                return Unauthorized();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return Ok(new MessageViewModel("Revoked"));
        }

        private bool TeamExists(int id)
        {
            return _context.Teams.Any(e => e.Id == id);
        }
    }
}
