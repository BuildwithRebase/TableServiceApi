using System;
using System.Security.Principal;
using System.Security.Claims;
using TableService.Core.Models;

namespace TableService.Core.Security
{
    /// <summary>
    /// Defines the data structure for returning a UserIdentity on a successful signon
    /// </summary>
    public static class ClaimsIdentityFactory
    {
        public static ClaimsIdentity ClaimsIdentityFromUser(User user)
        {
            var userIdentity = new UserIdentity(user.UserName);
            if (user.IsSuperAdmin) 
            {
                return new ClaimsIdentity(userIdentity, new Claim[] {
                    new Claim("team_name", user.TeamName),
                    new Claim("user_role", "SuperAdmin"),
                    new Claim("user_role", "Admin")
                });
            }
            else if (user.IsAdmin)
            {
                return new ClaimsIdentity(userIdentity, new Claim[] {
                    new Claim("team_name", user.TeamName),
                    new Claim("user_role", "Admin")
                });
            }
            else 
            {
                return new ClaimsIdentity(userIdentity, new Claim[] {
                    new Claim("team_name", user.TeamName),
                    new Claim("user_role", "General")
                });
            }
        }
    }
}
