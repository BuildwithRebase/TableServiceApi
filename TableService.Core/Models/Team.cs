using System;
using System.Collections.Generic;
using System.Text;

namespace TableService.Core.Models
{
    /// <summary>
    /// Defines the data structure for capturing and storing team details
    /// </summary>
    public class Team
    {
        public int Id { get; set; }
        public int? ParentTeamId { get; set; }
        public string TeamName { get; set; }
        public string ContactUserName { get; set; }
        public string ContactEmail { get; set; }
        public bool IsAdmin { get; set; }
        public string TablePrefix { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
