namespace TableService.Core.Messages
{
    public record SubscriberLoginRequest(int TeamId, string Email, string Password);
}
