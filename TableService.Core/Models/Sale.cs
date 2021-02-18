using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableService.Core.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string CustomerName { get; set; }
        public int PlanId { get; set; }
        public string SaleAmount { get; set; }
        public string SaleDate { get; set; }
        public string PlanName { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
