namespace TableService.Core.Messages
{
    public record UnsubscribeUserRequest(int TeamId, string Email);
}
