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
        public static ClaimsIdentity ClaimsIdentityFromSubscriber(Subscriber subscriber)
        {
            var userIdentity = new UserIdentity(subscriber.Email);
            return new ClaimsIdentity(userIdentity, new Claim[]
            {
                new Claim("user_type", "Subscriber"),
                new Claim("team_id", subscriber.TeamId.ToString()),
                new Claim("session_token", subscriber.SessionToken)
            });
        }

        public static ClaimsIdentity ClaimsIdentityFromUser(User user)
        {
            var userIdentity = new UserIdentity(user.Email);
            return new ClaimsIdentity(userIdentity, new Claim[] {
                new Claim("user_type", "User"),
                new Claim("team_id", user.TeamId.ToString()),
                new Claim("session_token", user.SessionToken)
            });
        }
    }
}
