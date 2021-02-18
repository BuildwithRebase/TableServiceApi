using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableServiceApi.ViewModels;

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
        public async Task<ActionResult<PagedResponseViewModel>> GetApiSessions([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;


            int totalCount = await _context.ApiSessions.Where(session => session.TeamId == teamId).CountAsync();
            var data = await _context.ApiSessions.Where(session => session.TeamId == teamId).Skip(skip).Take(take).ToListAsync();

            var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data);

            return response;
        }

        // GET: api/ApiSessions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiSession>> GetApiSession(int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            var session = await _context.ApiSessions.Where(s => s.TeamId == teamId && s.Id == id).FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound();
            }

            return session;
        }

        [HttpPut("{id}/revoke")]
        public async Task<IActionResult> RevokeSession(int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            var session = await _context.ApiSessions.Where(s => s.TeamId == teamId && s.Id == id).FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound();
            }

            session.IsActive = false;
            _context.Entry(session).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok(new MessageViewModel("Revoked"));
        }
    }
}
