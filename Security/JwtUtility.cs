using System;
using System.Security.Principal;
using System.Security.Claims;
using TableService.Core.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace TableService.Core.Security
{
    /// <summary>
    /// Defines the data structure for returning a UserIdentity on a successful signon
    /// </summary>
    public static class JwtUtility
    {
        // to-do move this into configuration
        private static string mySecret = "asdv234234^&%&^%&^hjsdfb2%%%";
        private static string myIssuer = "http://admin.buildwithrebase.online";
        private static string myAudience = "http://admin.buildwithrebase.online";

        public static string GenerateToken(ClaimsIdentity claimsIdentity)
        {
        	var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

        	var tokenHandler = new JwtSecurityTokenHandler();
        	var tokenDescriptor = new SecurityTokenDescriptor
        	{
        		Subject = claimsIdentity,
        		Expires = DateTime.UtcNow.AddDays(7),
        		Issuer = myIssuer,
        		Audience = myAudience,
        		SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
        	};

        	var token = tokenHandler.CreateToken(tokenDescriptor);
        	return tokenHandler.WriteToken(token);
        }

        public static bool ValidateCurrentToken(string token)
        {
            var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = myIssuer,
                    ValidAudience = myAudience,
                    IssuerSigningKey = mySecurityKey
                }, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}


