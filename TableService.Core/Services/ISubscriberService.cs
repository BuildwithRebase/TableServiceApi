using System.Threading.Tasks;
using TableService.Core.Messages;
using TableService.Core.Models;
using TableServiceApi.ViewModels;
using System.Collections.Generic;

namespace TableService.Core.Services
{
    public interface ISubscriberService
    {
        Task<ChangePasswordResponse> ChangePassword(SubscriberChangePasswordRequest request, ApiSession apiSession);
        Task<int> CreateSubscriber(ApiSession apiSession, SubscriberAddRequest subscriber);
        Task CreateTable(string tablePrefix);
        Task<bool> DeleteSubscriber(int id, string tablePrefix);
        Task<Subscriber> GetSubscriberByEmail(string tablePrefix, string email);
        Task<SubscriberDetailResponse> GetSubscriberById(ApiSession apiSession, int id);
        Task<PagedResponse<SubscriberListResponse>> GetSubscribers(string tablePrefix, int? page, int? pageSize);
        Task<Team> GetTeamByIdAsync(int id);
        Task<LoginResponse> Login(SubscriberLoginRequest request);
        Task<bool> Logout(ApiSession apiSession);
        Task<SubscribeResponse> Subscribe(SubscribeUserRequest subscribeUserRequest);
        Task<bool> Unsubscribe(UnsubscribeUserRequest request, ApiSession apiSession);
        Task<bool> UpdateSubscriber(ApiSession apiSession, int id, SubscriberUpdateRequest subscriber);
        Task<bool> ResetSubscriberPassword(ApiSession apiSession, int id, SubscriberResetPasswordRequest request);
        Task<int> SubmitForm(ApiSession apiSession, string tableName, Dictionary<string, object> data);
        Task<PagedResponse<Dictionary<string, object>>> GetForms(ApiSession apiSession, string tableName, int? page, int? pageSize, string select, string filter);
        Task<Dictionary<string, object>> GetForm(ApiSession apiSession, string tableName, int id);

    }
}