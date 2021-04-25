using System;
using System.Collections.Generic;
using System.Text;

namespace TableService.Core.Models
{
    /// <summary>
    /// Defines the data structure for capturing and storing user details
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string UserPassword { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public int LoginAttempts { get; set; }
        public string SessionToken { get; set; }
        public DateTime SessionTokenExpiry { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public bool Locked { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
