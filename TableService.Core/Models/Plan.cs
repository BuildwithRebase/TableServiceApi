using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableService.Core.Models
{
    public class Plan
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public int DisplayOrder { get; set; }
        public string PlanName { get; set; }
        public string BillingFrequency { get; set; }
        public string UserCount { get; set; }
        public string MonthlyCost { get; set; }
        public string AnnualCost { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
