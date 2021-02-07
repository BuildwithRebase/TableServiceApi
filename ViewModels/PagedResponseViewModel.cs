using System;
using System.Collections;

namespace TableServiceApi.ViewModels
{
    public class PagedResponseViewModel
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public int pages { get; set; }
        public int totalCount { get; set; }
        public int recordStart { get; set; }
        public int recordEnd { get; set; }
        public object data { get; set; }


        public PagedResponseViewModel(int page, int pageSize, int totalCount, object data)
        {
            this.page = page;
            this.pageSize = pageSize;
            this.recordStart = ((page - 1) * pageSize) + 1;
            this.recordEnd = Math.Min(totalCount, (this.recordStart + pageSize - 1));

            double pages = (double)totalCount / (double)pageSize;
            this.pages = (int) Math.Ceiling(pages);
            
            this.totalCount = totalCount;
            this.data = data;
        }
    }
}
