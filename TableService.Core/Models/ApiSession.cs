using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableService.Core.Models
{
    public class ApiSession
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string UserRoles { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
