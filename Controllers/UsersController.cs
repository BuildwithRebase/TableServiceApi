using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TableService.Core.Contexts;
using TableService.Core.Models;
using TableServiceApi.ViewModels;
using TableService.Core.Utility;
using TableService.Core.Security;
using TableServiceApi.Filters;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using TableService.Core.Types;
using TableServiceApi.Messages;
using TableService.Core.Messages;
using TableService.Core.Services;

namespace TableServiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly TableServiceContext _context;

        public UsersController(TableServiceContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<PagedResponse<UserViewModel>>> GetUsers([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;


            var teamId = apiSession.TeamId;
            int totalCount = await _context.Users.Where(user => user.TeamId == teamId).CountAsync();
            var data = await _context
                .Users
                .Where(user => user.TeamId == teamId)
                .Skip(skip)
                .Take(take)
                .Select(user => new UserViewModel(user.Id, user.UserName, user.Email, user.FirstName, user.LastName, user.TeamId, user.TeamName, user.CreatedAt, user.CreatedUserName, user.UpdatedAt, user.UpdatedUserName))
                .ToListAsync();

            int pages = PagedResponseUtility.GetPages(totalCount, take);
            int recordStart = PagedResponseUtility.RecordStart(skip + 1, take);
            int recordEnd = PagedResponseUtility.RecordEnd(totalCount, recordStart, take);

            var response = new PagedResponse<UserViewModel>(page ?? 1, pageSize ?? 10, pages, totalCount, recordStart, recordEnd, data);

            return response;
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserViewModel>> GetUser(int id)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            var user = await _context.Users.Where(user => user.TeamId == teamId && user.Id == id).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return new UserViewModel(user.Id, user.UserName, user.Email, user.FirstName, user.LastName, user.TeamId, user.TeamName, user.CreatedAt, user.CreatedUserName, user.UpdatedAt, user.UpdatedUserName);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("registerUser")]
        public async Task<ActionResult<UserViewModel>> PostRegisterUser(RegisterUserViewModel registerUser)
        {
            // validate the request
            if (string.IsNullOrEmpty(registerUser.UserName) ||
                string.IsNullOrEmpty(registerUser.UserPassword) ||
                string.IsNullOrEmpty(registerUser.Email) ||
                string.IsNullOrEmpty(registerUser.TeamName))
            {
                return BadRequest("You must provide User name, User password, Email and Team name");                
            }

            if (UserExistsByNameOrEmail(registerUser.UserName, registerUser.Email))
            {
                return Conflict("A user with user name: " + registerUser.UserName + " or email: " + registerUser.Email + " already exists");
            }

            Team team = FindTeamByName(registerUser.TeamName);
            bool isAdmin = false;
            if (team == null)
            {
                isAdmin = true;
                team = CreateTeam(registerUser.TeamName, 1, registerUser.Email, registerUser.UserName);

                SubscriberService subscriberService = new SubscriberService();
                await subscriberService.CreateTable(team.TablePrefix);
            }

            User user = new User
            {
                UserName = registerUser.UserName,
                Email = registerUser.Email,
                UserPassword = PasswordUtility.HashPassword(registerUser.UserPassword),
                IsAdmin = isAdmin,
                IsSuperAdmin = false,
                TeamId = team.Id,
                TeamName = team.TeamName,
                CreatedUserName = registerUser.UserName,
                UpdatedUserName = registerUser.UserName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            if (!string.IsNullOrEmpty(registerUser.FirstName))
            {
                user.FirstName = registerUser.FirstName;
            }
            if (!string.IsNullOrEmpty(registerUser.LastName))
            {
                user.LastName = registerUser.LastName;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return _context.Users.Where(u => u.UserName == registerUser.UserName).Select(u => UserViewModelFromUser(u)).FirstOrDefault();
  
        }

        // PUT: api/Users/updateUser/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("updateUser/{id}")]
        public async Task<IActionResult> PutData([FromRoute] int id, [FromBody] EditUserMessage message)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];

            if (!apiSession.IsAdmin)
            {
                return Unauthorized();
            }

            var user = _context.Users.Where(user => user.TeamId == apiSession.TeamId && user.Id == id).SingleOrDefault();
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (!user.FirstName.Equals(message.FirstName))
            {
                user.FirstName = message.FirstName;
            }
            if (!user.LastName.Equals(message.LastName))
            {
                user.LastName = message.LastName;
            }
            user.UpdatedUserName = apiSession.Email;
            user.UpdatedAt = DateTime.Now;

            _context.Entry(user).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return Ok(new MessageViewModel("User updated"));
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("authenticateUser")]
        public async Task<ActionResult<LoginResponse>> PostAuthenticateUser(AuthenticateUserViewModel authenticateUser)
        {
            // validate the request
            if (string.IsNullOrEmpty(authenticateUser.UserPassword) ||
                (string.IsNullOrEmpty(authenticateUser.UserName) && string.IsNullOrEmpty(authenticateUser.Email)))
            {
                return BadRequest("You must provide User passsword and one of User Name or Email");                
            }

            // Find the user
            User user = (string.IsNullOrEmpty(authenticateUser.UserName)) ? FindUserByEmail(authenticateUser.Email) : FindUserByName(authenticateUser.UserName);
            if (user == null)
            {
                return NotFound("Unable to logon with the credentials you provided");
            }
            if (user.Locked)
            {
                return Unauthorized("Account is locked");
            }

            // Authenticate the user using password
            bool valid = PasswordUtility.VerifyPassword(authenticateUser.UserPassword, user.UserPassword);
            if (!valid)
            {
                user.LoginAttempts++;
                user.Locked = (user.LoginAttempts >= 5);

                _context.Update(user);
                await _context.SaveChangesAsync();

                return Unauthorized("Unable to logon with the credentials you provided");
            }

            user.SessionToken = GuidHelper.CreateCryptographicallySecureGuid().ToString();
            user.SessionTokenExpiry = DateTime.Now.AddMinutes(30);
            user.LastAccessedAt = DateTime.Now;
            user.Locked = false;
            user.LoginAttempts = 0;

            _context.Update(user);
            await _context.SaveChangesAsync();

            // Get the Jwt Token
            var claimsIdentity = ClaimsIdentityFactory.ClaimsIdentityFromUser(user);
            var token = JwtUtility.GenerateToken(claimsIdentity);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Get the tables that belong to this team
            var tables = _context.Tables
                .Where(tbl => tbl.TeamId == user.TeamId)
                .Select(tbl => new TeamTable { Id = tbl.Id, TableName = tbl.TableName, TableLabel = tbl.TableLabel, FieldNames = tbl.FieldNames, FieldTypes = tbl.FieldTypes, TableState = tbl.TableState });

            HttpContext.Response.Cookies.Append("Authorization", "Bearer " + token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
            HttpContext.Response.Headers.Append("Token", token);

            return new LoginResponse(token, user.Id, user.Email, user.FirstName, user.LastName, user.TeamId, user.TeamName, tables.ToList());
        }

        [Authorize]
        [HttpGet]
        [Route("teamTables")]
        public ActionResult<List<TeamTable>> GetUserTeamTables()
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;
            // Get the tables that belong to this team
            var tables = _context.Tables
                .Where(tbl => tbl.TeamId == teamId && tbl.TableState != TableStateType.TableDeleted)
                .Select(tbl => new TeamTable { Id = tbl.Id, TableName = tbl.TableName, TableLabel = tbl.TableLabel, FieldNames = tbl.FieldNames, FieldTypes = tbl.FieldTypes, TableState = tbl.TableState } );

            return Ok(tables);
        }

        [Authorize]
        [HttpPut]
        [Route("logout")]
        public async Task<IActionResult> LogoutUser()
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.Where(user => user.Email == apiSession.Email && user.TeamId == apiSession.TeamId).FirstOrDefaultAsync();
            if (user != null)
            {
                user.SessionToken = "";
                await _context.SaveChangesAsync();
            }

            HttpContext.Response.Cookies.Delete("Authorization", new CookieOptions { HttpOnly = true });
            return Ok(new LogoutResponseViewModel());
        }

        private static UserViewModel UserViewModelFromUser(User user)
        {
            return new UserViewModel(user.Id, user.UserName, user.Email, user.FirstName, user.LastName, user.TeamId, user.TeamName, user.CreatedAt, user.CreatedUserName, user.UpdatedAt, user.UpdatedUserName);
        }

        private Team FindTeamByName(string teamName)
        {
            return _context.Teams.Where(t => t.TeamName == teamName).FirstOrDefault<Team>();    
        }

        private Team CreateTeam(string teamName, int? parentTeamId, string contactEmail, string userName)
        {
            Team team = new Team 
            {
                ParentTeamId = parentTeamId,
                TeamName = teamName,
                ContactUserName = userName,
                ContactEmail = contactEmail,
                TablePrefix = TableUtility.GetTablePrefixFromName(teamName),
                IsAdmin = false,
                CreatedUserName = userName,
                UpdatedUserName = userName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Teams.Add(team);
            _context.SaveChanges();

            return FindTeamByName(teamName);
        }
        

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        private bool UserExistsByNameOrEmail(string userName, string email)
        {
            return _context.Users.Any(u => u.UserName == userName || u.Email == email);
        }

        private User FindUserByName(string userName)
        {
            return _context.Users.Where(u => u.UserName == userName).FirstOrDefault();
        }

        private User FindUserByEmail(string email)
        {
            return _context.Users.Where(u => u.Email == email).FirstOrDefault();
        }

        private static string FormatUserRoles(User user)
        {
            StringBuilder roles = new StringBuilder();
            roles.Append("GeneralUser");
            if (user.IsAdmin)
            {
                roles.Append(",").Append("Admin");
            }
            if (user.IsSuperAdmin)
            {
                roles.Append(",").Append("SuperAdmin");
            }
            return roles.ToString();
        }
    }
}
