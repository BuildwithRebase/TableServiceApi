using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public class JwtUserViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string UserRoles { get; set; }
        public string Token { get; set; }
        public List<TeamTable> Tables { get; set; }
    }
}
