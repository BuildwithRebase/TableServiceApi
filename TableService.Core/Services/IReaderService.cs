using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Models;
using TableServiceApi.ViewModels;

namespace TableService.Core.Services
{
    public interface IReaderService
    {
        Task<PagedResponse<Dictionary<string, object>>> GetForms(int teamId, string tableName, int? page, int? pageSize, string select, string filter);
        Task<Dictionary<string, object>> GetForm(int teamId, string tableName, int id);
    }
}
