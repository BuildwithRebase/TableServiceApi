using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public class TableRecordSearchResponseViewModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<Dictionary<string, object>> RecordData { get; set; }
    }
}
