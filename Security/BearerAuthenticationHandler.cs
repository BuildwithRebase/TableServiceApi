using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using TableService.Core.Contexts;
using Dapper;
using Dapper.Contrib.Extensions;
using TableService.Core.Models;

namespace TableService.Core.Security
{
    public class BearerAuthenticationOptions : AuthenticationSchemeOptions
    {
    }

    public class BearerAuthenticationHandler : AuthenticationHandler<BearerAuthenticationOptions>
    {
        private TableServiceContext _context;
        public BearerAuthenticationHandler(IOptionsMonitor<BearerAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, TableServiceContext context) : base(options, logger, encoder, clock)
        {
            this._context = context;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authorizationHeader = null;
            if (Request.Cookies.ContainsKey("Authorization"))
            {
                authorizationHeader = Request.Cookies["Authorization"];
            }

            if (authorizationHeader == null && !Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Unauthorized");
            }

            if (authorizationHeader == null)
            {
                authorizationHeader = Request.Headers["Authorization"];
            }

            if (string.IsNullOrEmpty(authorizationHeader))
            {
                return AuthenticateResult.NoResult();
            }

            if (!authorizationHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Unauthorized");
            }

            string token = authorizationHeader.Substring("bearer".Length).Trim();

            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Fail("Unauthorized");
            }

            try
            {
                return await validateToken(token);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex.Message);
            }
        }

        private async Task<ApiSession> ValidateUserToken(string email, int teamId, string sessionToken)
        {
            var sql = @"SELECT * FROM Users WHERE Email = @Email AND TeamId = @TeamId AND SessionToken = @SessionToken";
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email, TeamId = teamId, SessionToken = sessionToken });
            if (user != null && !user.Locked)
            {
                var team = await GetTeamAsync(teamId);
                var apiSession = new ApiSession(email, user.FirstName + " " + user.LastName, user.TeamId,
                    team.TeamName, team.TablePrefix, false, user.IsSuperAdmin, user.IsAdmin);
                return apiSession;
            }
            return null;
        }

        private async Task<ApiSession> ValidateSubscriberToken(string email, int teamId, string sessionToken)
        {

            var team = await GetTeamAsync(teamId);

            var sql = string.Format(@"SELECT * FROM {0}_subscribers WHERE Email = @Email AND TeamId = @TeamId AND SessionToken = @SessionToken", team.TablePrefix);
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            var subscriber = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email, TeamId = teamId, SessionToken = sessionToken });
            if (subscriber != null && !subscriber.Locked && subscriber.SessionTokenExpiry >= DateTime.Now)
            {

                var apiSession = new ApiSession(email, subscriber.FirstName + " " + subscriber.LastName, subscriber.TeamId, 
                    team.TeamName, team.TablePrefix, true, subscriber.IsSuperAdmin, subscriber.IsAdmin);
                return apiSession;
            }
            return null;
        }

        private async Task<Team> GetTeamAsync(int teamId)
        {
            var sql = @"SELECT * FROM Teams WHERE Id = @Id";
            using var connection = new MySqlConnection(TableServiceContext.ConnectionString);
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<Team>(sql, new { Id = teamId });
        }

        private async Task<AuthenticateResult> validateToken(string token)
        {
            ClaimsPrincipal principal = JwtUtility.ValidateCurrentToken(token);
            if (principal == null)
            {
                return AuthenticateResult.Fail("Unauthorized");
            }

            int teamId = 0;
            string userType = "";
            string sessionToken = "";
            foreach (var claim in principal.Claims)
            {
                switch (claim.Type)
                {
                    case "team_id":
                        teamId = Int32.Parse(claim.Value);
                        break;
                    case "user_type":
                        userType = claim.Value;
                        break;
                    case "session_token":
                        sessionToken = claim.Value;
                        break;
                }
            }

            var apiSession = (userType == "Subscriber") ? 
                await ValidateSubscriberToken(principal.Identity.Name, teamId, sessionToken) : 
                await ValidateUserToken(principal.Identity.Name, teamId, sessionToken);

            if (apiSession == null)
            {
                return AuthenticateResult.Fail("You have not provided a valid access token");
            }

            Context.Items.Add("api_session", apiSession);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}