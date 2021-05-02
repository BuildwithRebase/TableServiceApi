namespace TableService.Core.Messages
{
    public record SubscriberUpdateRequest(int SubscriberId, string Email, string FirstName, string LastName, bool Subscribed);
}
