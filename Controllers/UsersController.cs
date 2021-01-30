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
        public async Task<ActionResult<IEnumerable<UserViewModel>>> GetUsers()
        {
            IEnumerable<User> users = await _context.Users.ToListAsync();

            return users.Select(u => new UserViewModel { Email = u.Email, Id = u.Id, FirstName = u.FirstName, LastName = u.LastName, TeamId = u.TeamId, UserName = u.UserName, TeamName = u.TeamName }).ToList();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserViewModel>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

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
        public ActionResult<JwtUserViewModel> PostAuthenticateUser(AuthenticateUserViewModel authenticateUser)
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

            return JwtUserViewModelFromUser(user, token);
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutUser(int id, User user)
        //{
        //    if (id != user.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(user).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!UserExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //       s     throw;
        //        }
        //    }

        //    return NoContent();
        //}

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<User>> PostUser(User user)
        //{
        //    _context.Users.Add(user);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetUser", new { id = user.Id }, user);
        //}

        // DELETE: api/Users/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteUser(int id)
        //{
        //    var user = await _context.Users.FindAsync(id);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Users.Remove(user);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

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

        private static JwtUserViewModel JwtUserViewModelFromUser(User user, string token)
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
                Token = token
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
    }
}
