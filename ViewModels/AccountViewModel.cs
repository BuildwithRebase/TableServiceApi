using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public class AccountViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool UserIsAdmin { get; set; }
        public bool UserIsSuperAdmin { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string ContactUserName { get; set; }
        public string ContactEmail { get; set; }
        public string BillFlowSecret { get; set; }
        public bool IsAdmin { get; set; }
        public string TablePrefix { get; set; }
        public string UserRole { get; set; }
    }
}
