namespace TableService.Core.Messages
{
    public record SubscriberChangePasswordRequest(int TeamId, string Email, string CurrentPassword, string NewPassword, string NewPasswordConfirm);
}
