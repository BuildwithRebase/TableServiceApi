
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using TableService.Core.Security;

namespace TableServiceApi.Filters
{
    public class MyAuthorization: AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string userRole;
        public MyAuthorization() 
        {
            this.userRole = "General";
        }

        public MyAuthorization(string userRole)
        {
            this.userRole = userRole;
        }

        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            StringValues value;
            filterContext.HttpContext.Request.Headers.TryGetValue("Authorization", out value);
            if (value.Count == 0)
            {
                filterContext.Result = new UnauthorizedResult();
                return;
            }

            var token = value.ToArray()[0];
            if (!JwtUtility.ValidateCurrentToken(token))
            {
                filterContext.Result = new UnauthorizedResult();
            }
        }
    }

}

