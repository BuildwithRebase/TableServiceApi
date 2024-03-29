﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableServiceApi.ViewModels;

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
        // public ActionResult<PingResponseViewModel> GetPing([FromQuery] string createToken)
        public ActionResult<PingResponseViewModel> GetPing()
        {
/*           
 *           Uncomment this only if you want to recreate the entire database from scratch (not recommended)
 *           if ("bC4hSZxB" == createToken)
            {
                _context.Database.EnsureDeleted();
                _context.Database.EnsureCreated();

                return Ok(new PingResponseViewModel { Message = "Database created", Authorized = false });
            } */

            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            return Ok(new PingResponseViewModel { Message = "ok", Authorized = (apiSession != null) });
        }
    }
}
