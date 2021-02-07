using System;
using System.Collections.Generic;
using System.Text;

namespace TableService.Core.Models
{
    public class Table
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TableName { get; set; }
        public int RowCount { get; set; }
        public string TableLabel { get; set; }
        public string FieldNames { get; set; }
        public string FieldTypes { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
        public override string ToString()
        {
            return String.Format("{0}, fields: {0}, types: {1}", this.TableName, this.FieldNames, this.FieldTypes);
        }
    }
}
