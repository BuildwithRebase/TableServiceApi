using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Types;

namespace TableServiceApi.ViewModels
{
    public record UpdateTableRequestViewModel
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string FieldNames { get; set; }
        public string FieldTypes { get; set; }
        public TableStateType? TableState { get; set; }
        public TablePrivacyModelType? TablePrivacyModel { get; set; }
        public TableViewModeType? TableViewMode { get; set; }
    }
}
