using System;

namespace TableService.Core.Messages
{
    public record SubscriberListResponse(int Id, int TeamId, string Email, string FirstName, string LastName, bool Subscribed, DateTime LastAccessedAt, DateTime SubscribedAt);
}
