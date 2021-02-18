using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableServiceApi.ViewModels;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly TableServiceContext _context;

        public AccountController(TableServiceContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Gets the available tables and definitions
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<AccountViewModel>> Get()
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];

            var team = _context.Teams.Where(table => table.Id == apiSession.TeamId).FirstOrDefault();
            var user = _context.Users.Where(user => user.UserName == apiSession.UserName).FirstOrDefault();

            var account = new AccountViewModel()
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserIsAdmin = user.IsAdmin,
                UserIsSuperAdmin = user.IsSuperAdmin,
                TeamId = team.Id,
                TeamName = team.TeamName,
                ContactUserName = team.ContactUserName,
                ContactEmail = team.ContactEmail,
                BillFlowSecret = team.BillFlowSecret,
                IsAdmin = team.IsAdmin,
                TablePrefix = team.TablePrefix,
                UserRole = apiSession.UserRoles
            };

            return Ok(account);
        }

        [HttpPost]
        [Authorize]
        [Route("updateBillFlowSecret")]
        public async Task<ActionResult<AccountViewModel>> UpdateBillFlowSecret(UpdateBillFlowSecretRequest request)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];

            var team = _context.Teams.Where(table => table.Id == apiSession.TeamId).FirstOrDefault();

            team.BillFlowSecret = request.BillFlowSecret;

            await _context.SaveChangesAsync();
            return await Get();
        }

        [HttpGet]
        [Authorize]
        [Route("getBillFlowHMAC")]
        public async Task<ActionResult<MessageViewModel>> GenerateHMAC()
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            var billFlowSecret = _context.Teams.Where(team => team.Id == apiSession.TeamId).Select(team => team.BillFlowSecret).FirstOrDefault();
            var email = _context.Users.Where(user => user.UserName == apiSession.UserName).Select(user => user.Email).FirstOrDefault();

            var hash = GetHash(email, billFlowSecret);

            return Ok(new MessageViewModel(hash));
        }

        public static String GetHash(String text, String key)
        {
            // change according to your needs, an UTF8Encoding
            // could be more suitable in certain situations
            ASCIIEncoding encoding = new ASCIIEncoding();

            Byte[] textBytes = encoding.GetBytes(text);
            Byte[] keyBytes = encoding.GetBytes(key);

            Byte[] hashBytes;

            using (HMACSHA256 hash = new HMACSHA256(keyBytes))
                hashBytes = hash.ComputeHash(textBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }


    }
}
