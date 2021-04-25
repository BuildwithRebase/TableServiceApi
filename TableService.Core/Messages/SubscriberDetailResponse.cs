using System;

namespace TableService.Core.Messages
{
    public record SubscriberDetailResponse(int Id, int TeamId, string Email, string FirstName, string LastName, bool Subscribed, DateTime LastAccessedAt, DateTime SubscribedAt, string CreatedUserName, string UpdatedUserName, DateTime CreatedAt, DateTime UpdatedAt);
}
