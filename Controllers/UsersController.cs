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
        public async Task<ActionResult<PagedResponseViewModel>> GetUsers([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var apiSession = (ApiSession)HttpContext.Items["api_session"];
            if (apiSession == null)
            {
                return Unauthorized();
            }

            var teamId = apiSession.TeamId;

            int take = (pageSize == null) ? 10 : (int)pageSize;
            int skip = (page == null) ? 0 : ((int)page - 1) * take;


            int totalCount = await _context.Users.Where(user => user.TeamId == teamId).CountAsync();
            var data = await _context
                .Users
                .Where(user => user.TeamId == teamId)
                .Skip(skip)
                .Take(take)
                .Select(u => new UserViewModel { Email = u.Email, Id = u.Id, FirstName = u.FirstName, LastName = u.LastName, TeamId = u.TeamId, UserName = u.UserName, TeamName = u.TeamName })
                .ToListAsync();

            var response = new PagedResponseViewModel(page ?? 1, pageSize ?? 10, totalCount, data, DynamicClassUtility.GetFieldDefinitions(typeof(User)));

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

            return new UserViewModel { Email = user.Email, Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, TeamId = user.TeamId, UserName = user.UserName, TeamName = user.TeamName };
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

        [HttpPost]
        [AllowAnonymous]
        [Route("authenticateUser")]
        public async Task<ActionResult<JwtUserViewModel>> PostAuthenticateUser(AuthenticateUserViewModel authenticateUser)
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

            // Authenticate the user using password
            bool valid = PasswordUtility.VerifyPassword(authenticateUser.UserPassword, user.UserPassword);
            if (!valid)
            {
                return Unauthorized("Unable to logon with the credentials you provided");
            }

            // Get the Jwt Token
            var claimsIdentity = ClaimsIdentityFactory.ClaimsIdentityFromUser(user);
            var token = JwtUtility.GenerateToken(claimsIdentity);

            // Invalidate any previous sessions
            InvalidateSessions(user.UserName);


            // Save the Api Session
            CreateApiSession(user);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Get the tables that belong to this team
            var tables = _context.Tables
                .Where(tbl => tbl.TeamId == user.TeamId)
                .Select(tbl => new TeamTable { Id = tbl.Id, TableName = tbl.TableName, TableLabel = tbl.TableLabel });

            HttpContext.Response.Cookies.Append("Authorization", "Bearer " + token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
            HttpContext.Response.Headers.Append("Token", token);

            return JwtUserViewModelFromUser(user, tables.ToList(), token);
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

            InvalidateSessions(apiSession.UserName);
            await _context.SaveChangesAsync();

            HttpContext.Response.Cookies.Delete("Authorization", new CookieOptions { HttpOnly = true });
            return Ok(new LogoutResponseViewModel());
        }

       private static UserViewModel UserViewModelFromUser(User user)
        {
            return new UserViewModel 
            { 
                Email = user.Email, 
                Id = user.Id, 
                FirstName = user.FirstName, 
                LastName = user.LastName, 
                TeamId = user.TeamId, 
                UserName = user.UserName, 
                TeamName = user.TeamName 
            };
        }

        private static JwtUserViewModel JwtUserViewModelFromUser(User user, List<TeamTable> tables, string token)
        {
            return new JwtUserViewModel
            {
                Email = user.Email,
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TeamId = user.TeamId,
                UserName = user.UserName,
                TeamName = user.TeamName,
                UserRoles = FormatUserRoles(user),
                Token = token,
                Tables = tables
            };
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

        /// <summary>
        /// Invalidate any old sessions
        /// </summary>
        /// <param name="userName"></param>
        private void InvalidateSessions(string userName)
        {
            var sessions = _context.ApiSessions.Where(session => session.UserName == userName && session.IsActive).ToList();
            if (sessions.Count == 0)
            {
                return;
            }
            foreach (var session in sessions)
            {
                session.IsActive = false;

                _context.ApiSessions.Update(session);
            }
        }

        private void CreateApiSession(User user)
        {
            ApiSession session = new ApiSession
            {
                UserName = user.UserName,
                TeamId = user.TeamId,
                TeamName = user.TeamName,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            session.UserRoles = FormatUserRoles(user);

            _context.ApiSessions.Add(session);
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
