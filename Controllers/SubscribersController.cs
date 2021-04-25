using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private SubscriberService subscriberService;

        public SubscribersController(SubscriberService subscriberService)
        {
            this.subscriberService = subscriberService;
        }

        [HttpPost("subscribe")]
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

        [HttpPost("unsubscribe")]
        [Authorize]
        public async Task<ActionResult<bool>> Unsubscribe(UnsubscribeUserRequest request)
        {
            try
            {
                return await subscriberService.Unsubscribe(request);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(SubscriberLoginRequest request)
        {
            try
            {
                var response = await subscriberService.Login(request);
                HttpContext.Response.Cookies.Append("Authorization", "Bearer " + response.jwt, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
                return response;
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(SubscriberChangePasswordRequest request)
        {
            try
            {
                return await subscriberService.ChangePassword(request);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }

        }

        [HttpGet("")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<SubscriberListResponse>>> GetSubscribers([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            try
            {
                return await subscriberService.GetSubscribers(null, page, page);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }

        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<SubscriberDetailResponse>> GetSubscriber(int id)
        {
            try
            {
                return await subscriberService.GetSubscriberById(null, id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }

        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SubscriberDetailResponse>> CreateSubscriber(Subscriber subscriber)
        {
            try
            {
                int id = await subscriberService.CreateSubscriber(subscriber, null);
                return await GetSubscriber(id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }

        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SubscriberDetailResponse>> UpdateSubscriber(Subscriber subscriber)
        {
            try
            {
                await subscriberService.UpdateSubscriber(subscriber, null);
                return await GetSubscriber(subscriber.Id);
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }

        }

        [HttpDelete]
        [Authorize]
        public async Task<ActionResult<bool>> DeleteSubscriber(Subscriber subscriber)
        {
            try
            {
                await subscriberService.DeleteSubscriber(subscriber, null);
                return true;
            }
            catch (MyHttpException ex)
            {
                return StatusCode(ex.HttpStatusCode, ex.Message);
            }

        }
    }
}
