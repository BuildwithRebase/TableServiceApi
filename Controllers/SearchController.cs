using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using TableServiceApi.TableService.Core.Contexts;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly TableServiceContext _context;
        private readonly TeamDbContext _teamDbContext;

        public SearchController(TableServiceContext context, TeamDbContext teamDbContext)
        {
            _context = context;
            _teamDbContext = teamDbContext;
        }

        // GET: api/TableRecords
        [HttpGet]
        public async Task<ActionResult<IEnumerable>> Search([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            _teamDbContext.Database.EnsureCreated();

            int skip = (page == null) ? 0 : (int) page;
            int take = (pageSize == null) ? 10 : (int) pageSize;

            var results = await _teamDbContext.TableRecords.Skip(skip).Take(take).ToListAsync();

            // to-do capture that the user has run a query

            return results;
        }

    }
}
