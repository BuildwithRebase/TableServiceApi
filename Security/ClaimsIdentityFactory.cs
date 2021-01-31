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
                    new Claim("UserRole", "SuperAdmin"),
                    new Claim("UserRole", "Admin")
                });
            }
            else if (user.IsAdmin)
            {
                return new ClaimsIdentity(userIdentity, new Claim[] {
                    new Claim("UserRole", "Admin")
                });
            }
            else 
            {
                return new ClaimsIdentity(userIdentity, new Claim[] {
                    new Claim("UserRole", "General")
                });
            }
        }
    }
}
