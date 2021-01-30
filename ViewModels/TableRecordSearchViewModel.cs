using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public class TableRecordSearchViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TableName { get; set; }
        public List<string> Fields { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
