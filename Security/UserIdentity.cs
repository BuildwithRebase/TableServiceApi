using System;
using System.Security.Principal;

namespace TableService.Core.Security
{
    /// <summary>
    /// Defines the data structure for returning a UserIdentity on a successful signon
    /// </summary>
    public class UserIdentity : IIdentity
    {
        public string AuthenticationType { get;set; }

        public bool IsAuthenticated { get; set; }

        public string Name { get; set; }

        public UserIdentity(string userName)
        {
            this.AuthenticationType = "local account";
            this.IsAuthenticated = true;
            this.Name = userName;
        }
    }
}
