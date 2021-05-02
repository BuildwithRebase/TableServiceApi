namespace TableService.Core.Messages
{
    public record SubscriberResetPasswordRequest(int SubscriberId, string Password, string ConfirmPassword);
}
