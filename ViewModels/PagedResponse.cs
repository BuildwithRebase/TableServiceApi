using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public record PagedResponse<T> (int Page, int PageSize, int Pages, int TotalCount, int RecordStart, int RecordEnd, IEnumerable<T> Data);
    public static class PagedResponseUtility
    {
        public static int GetPages(int totalCount, int pageSize) => totalCount / pageSize;
        public static int RecordStart(int page, int pageSize) => ((page - 1) * pageSize) + 1;
        public static int RecordEnd(int totalCount, int recordStart, int pageSize) => Math.Min(totalCount, (recordStart + pageSize - 1));
    }

    public static class PagedResponseHelper<T>
    {
        public static PagedResponse<T> CreateResponse(int? page, int? pageSize, int totalCount, IEnumerable<T> data)
        {
            int pageValue = page ?? 1;
            int pageSizeValue = pageSize ?? 10;
            int pages = totalCount / pageSizeValue;
            int recordStart = ((pageValue - 1) * pageSizeValue) + 1;
            int recordEnd = Math.Min(totalCount, (recordStart + pageSizeValue - 1));

            return new PagedResponse<T>(pageValue, pageSizeValue, pages, totalCount, recordStart, recordEnd, data);
        }
    }
   
}
