namespace TableService.Core.Messages
{
    public record SubscribeUserRequest(int TeamId, string Email, string FirstName, string LastName, string Password, string ConfirmPassword);
}
