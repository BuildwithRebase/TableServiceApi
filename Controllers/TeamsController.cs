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
using TableService.Core.Services;
using TableService.Core.Utility;
using TableServiceApi.Filters;
using TableServiceApi.Messages;
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
        private readonly SubscriberService subscriberService;

        public TeamsController(TableServiceContext context)
        {
            _context = context;
            subscriberService = new SubscriberService();
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
            var teamId = apiSession.TeamId;

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;

            if (apiSession.IsSuperAdmin)
            {
                int totalCount = await _context.Teams.CountAsync();
                var data = await _context.Teams.Skip(skip).Take(take).ToListAsync();

                var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data, DynamicClassUtility.GetFieldDefinitions(typeof(Team)));

                return response;
            } 
            else
            {
                int totalCount = await _context.Teams.CountAsync();
                var data = await _context.Teams.Where(t => t.Id == apiSession.TeamId || t.ParentTeamId == apiSession.TeamId).Skip(skip).Take(take).ToListAsync();

                var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data, DynamicClassUtility.GetFieldDefinitions(typeof(Team)));

                return response;
            }
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
            if (!apiSession.IsSuperAdmin && apiSession.TeamId != id)
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
        public async Task<IActionResult> PutTeam(int id, EditTeamMessage message)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            if (apiSession.TeamId != id)
            {
                if (!apiSession.IsSuperAdmin)
                {
                    return Unauthorized();
                }
            }

            var team = _context.Teams.Where(team => team.Id == message.Id).SingleOrDefault();
            if (team == null)
            {
                return NotFound("Team not found");
            }

            if ((string.IsNullOrEmpty(team.ContactUserName) && !string.IsNullOrEmpty(message.ContactUserName)) ||
                (!string.IsNullOrEmpty(team.ContactUserName) && !team.ContactUserName.Equals(message.ContactUserName)))
            {
                team.ContactUserName = message.ContactUserName;
            }
            if ((string.IsNullOrEmpty(team.ContactEmail) && !string.IsNullOrEmpty(message.ContactEmail)) ||
                (!string.IsNullOrEmpty(team.ContactEmail) && !team.ContactEmail.Equals(message.ContactEmail)))
            {
                team.ContactEmail = message.ContactEmail;
            }
            if ((string.IsNullOrEmpty(team.BillFlowSecret) && !string.IsNullOrEmpty(message.BillFlowSecret)) ||
                (!string.IsNullOrEmpty(team.BillFlowSecret) && !team.ContactEmail.Equals(message.BillFlowSecret)))
            {
                team.BillFlowSecret = message.BillFlowSecret;
            }

            team.UpdatedAt = DateTime.Now;
            team.UpdatedUserName = apiSession.Email;

            _context.Entry(team).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return Ok(new MessageViewModel("Team updated"));
        }

        // POST: api/Teams
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Team>> PostTeam(AddTeamMessage message)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }
            if (apiSession.TeamId == 1 && !apiSession.IsSuperAdmin)
            {
                return Unauthorized();
            }
            if (string.IsNullOrEmpty(message.TeamName))
            {
                return BadRequest();
            }

            var existingTeam = _context.Teams.Where(tbl => tbl.TeamName == message.TeamName).SingleOrDefault();
            if (existingTeam != null)
            {
                return Conflict();
            }

            var team = new Team();

            team.TeamName = message.TeamName;
            team.ParentTeamId = apiSession.TeamId;
            team.IsAdmin = false;
            team.TablePrefix = TableUtility.GetTablePrefixFromName(team.TeamName);
            if (!string.IsNullOrEmpty(message.ContactUserName))
            {
                team.ContactUserName = message.ContactUserName;
            }    
            if (!string.IsNullOrEmpty(message.ContactUserEmail))
            {
                team.ContactEmail = message.ContactUserEmail;
            }
            if (!string.IsNullOrEmpty(message.BillFlowSecret))
            {
                team.BillFlowSecret = message.BillFlowSecret;
            }

            team.UpdatedAt = DateTime.Now;
            team.UpdatedUserName = apiSession.Email;
            team.CreatedAt = DateTime.Now;
            team.CreatedUserName = apiSession.Email;

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            await subscriberService.CreateTable(team.TablePrefix);

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
            if (!apiSession.IsSuperAdmin)
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
