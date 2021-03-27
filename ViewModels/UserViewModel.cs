using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public record UserViewModel (int Id, string UserName, string Email, string FirstName, string LastName, int TeamId, string TeamName, DateTime CreatedAt, string CreatedUserName, DateTime UpdatedAt, string UpdatedUserName);
}
