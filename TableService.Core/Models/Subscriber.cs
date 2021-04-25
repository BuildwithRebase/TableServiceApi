using System;

namespace TableService.Core.Models
{
    public class Subscriber
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
        public string SessionToken { get; set; }
        public DateTime SessionTokenExpiry { get; set; }
        public bool Subscribed { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public DateTime SubscribedAt { get; set; }
        public bool Locked { get; set; }
        public int LoginAttempts { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
