using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public record PagedResponse<T> (int page, int pageSize, int pages, int totalCount, int recordStart, int recordEnd, IEnumerable<T> data);
    public static class PagedResponseUtility
    {
        public static int GetPages(int totalCount, int pageSize) => totalCount / pageSize;
        public static int RecordStart(int page, int pageSize) => ((page - 1) * pageSize) + 1;
        public static int RecordEnd(int totalCount, int recordStart, int pageSize) => Math.Min(totalCount, (recordStart + pageSize - 1));
    }
}
