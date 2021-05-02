using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TableService.Core.Exceptions;
using TableService.Core.Messages;
using TableService.Core.Models;
using TableService.Core.Services;
using TableServiceApi.ViewModels;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscribersController : ControllerBase
    {
        private ISubscriberService subscriberService;

        public SubscribersController(ISubscriberService subscriberService)
        {
            this.subscriberService = subscriberService;
        }

        [HttpPost("actions/subscribe")]
        public async Task<ActionResult<SubscribeResponse>> Subscribe(SubscribeUserRequest request)
        {
            try
            {
                return await subscriberService.Subscribe(request);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("actions/unsubscribe")]
        [Authorize]
        public async Task<ActionResult<bool>> Unsubscribe(UnsubscribeUserRequest request)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || !apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                return await subscriberService.Unsubscribe(request, apiSession);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("actions/login")]
        public async Task<ActionResult<LoginResponse>> Login(SubscriberLoginRequest request)
        {
            try
            {
                var response = await subscriberService.Login(request);
                HttpContext.Response.Cookies.Append("Authorization", "Bearer " + response.Jwt, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
                return response;
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("actions/logout")]
        public async Task<ActionResult<bool>> Logout()
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || !apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }
                var result = await subscriberService.Logout(apiSession);
                HttpContext.Response.Cookies.Append("Authorization", "", new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
                return result;
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("actions/change-password")]
        [Authorize]
        public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(SubscriberChangePasswordRequest request)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || !apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                return await subscriberService.ChangePassword(request, apiSession);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpGet("members")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<SubscriberListResponse>>> GetSubscribers([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                return await subscriberService.GetSubscribers(apiSession.TablePrefix, page, pageSize);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpGet("members/{id}")]
        [Authorize]
        public async Task<ActionResult<SubscriberDetailResponse>> GetSubscriber(int id)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                return await subscriberService.GetSubscriberById(apiSession, id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("members")]
        [Authorize]
        public async Task<ActionResult<SubscriberDetailResponse>> CreateSubscriber(SubscriberAddRequest request)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                int id = await subscriberService.CreateSubscriber(apiSession, request);
                return await GetSubscriber(id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPut("members/{id}")]
        [Authorize]
        public async Task<ActionResult<SubscriberDetailResponse>> UpdateSubscriber([FromRoute] int id, SubscriberUpdateRequest request)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                await subscriberService.UpdateSubscriber(apiSession, id, request);
                Subscriber subscriber = await subscriberService.GetSubscriberByEmail(apiSession.TablePrefix, request.Email);

                return SubscriberService.MapDetailResponseFromSubscriber(subscriber);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPut("members-reset-password/{id}")]
        [Authorize]
        public async Task<ActionResult<SubscriberDetailResponse>> ResetSubscriberPassword([FromRoute] int id, SubscriberResetPasswordRequest request)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                await subscriberService.ResetSubscriberPassword(apiSession, id, request);

                return await subscriberService.GetSubscriberById(apiSession, id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpDelete("members/{id}")]
        [Authorize]
        public async Task<ActionResult<bool>> DeleteSubscriber(int id)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                await subscriberService.DeleteSubscriber(id, apiSession.TablePrefix);
                return true;
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("forms/{tableName}")]
        [Authorize]
        public async Task<ActionResult<int>> SubmitForm([FromRoute]string tableName, [FromBody] Dictionary<string, object> data)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || !apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                return await subscriberService.SubmitForm(apiSession, tableName, data);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpGet("forms/{tableName}")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<Dictionary<string,object>>>> GetForms([FromRoute] string tableName, [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string select, [FromQuery] string filter)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || !apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                return await subscriberService.GetForms(apiSession, tableName, page, pageSize, select, filter);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpGet("forms/{tableName}/{id}")]
        [Authorize]
        public async Task<ActionResult<Dictionary<string, object>>> GetForm([FromRoute] string tableName, [FromRoute] int id)
        {
            try
            {
                var apiSession = (ApiSession)HttpContext.Items["api_session"];
                if (apiSession == null || !apiSession.IsSubscriber)
                {
                    return Unauthorized();
                }

                return await subscriberService.GetForm(apiSession, tableName, id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }
    }
}
