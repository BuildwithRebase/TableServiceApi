using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Exceptions;
using TableService.Core.Services;
using TableServiceApi.ViewModels;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReadersController : ControllerBase
    {
        private readonly IReaderService readerService;

        public ReadersController(IReaderService readerService)
        {
            this.readerService = readerService;
        }

        [HttpGet("readers/{teamId}/{tableName}")]
        public async Task<ActionResult<PagedResponse<Dictionary<string, object>>>> GetForms([FromRoute] int teamId, [FromRoute] string tableName, [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string select, [FromQuery] string filter)
        {
            try
            {
                return await readerService.GetForms(teamId, tableName, page, pageSize, select, filter);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpGet("readers/{teamId}/{tableName}/{id}")]
        public async Task<ActionResult<Dictionary<string, object>>> GetForm([FromRoute] int teamId, [FromRoute] string tableName, [FromRoute] int id)
        {
            try
            {
                return await readerService.GetForm(teamId, tableName, id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

    }
}
