using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Contexts;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        private readonly TableServiceContext _context;

        public PingController(TableServiceContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<string> GetPing([FromQuery] string createToken)
        {
            if ("bC4hSZxB" == createToken)
            {
                _context.Database.EnsureCreated();
                return Ok("Database created");
            }
            
            return Ok("ok");
        }
    }
}
