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


// public string GenerateToken(int userId)
// {
// 	var mySecret = "asdv234234^&%&^%&^hjsdfb2%%%";
// 	var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

// 	var myIssuer = "http://mysite.com";
// 	var myAudience = "http://myaudience.com";

// 	var tokenHandler = new JwtSecurityTokenHandler();
// 	var tokenDescriptor = new SecurityTokenDescriptor
// 	{
// 		Subject = new ClaimsIdentity(new Claim[]
// 		{
// 			new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
// 		}),
// 		Expires = DateTime.UtcNow.AddDays(7),
// 		Issuer = myIssuer,
// 		Audience = myAudience,
// 		SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
// 	};

// 	var token = tokenHandler.CreateToken(tokenDescriptor);
// 	return tokenHandler.WriteToken(token);
// }

