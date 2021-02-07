using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableService.Core.Models
{
    public class TableRecord
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TableName { get; set; }
        public string Field1StringValue { get; set; }
        public int? Field1NumberValue { get; set; }
        public DateTime Field1DateTimeValue { get; set; }
        public string Field2StringValue { get; set; }
        public int? Field2NumberValue { get; set; }
        public DateTime Field2DateTimeValue { get; set; }
        public string Field3StringValue { get; set; }
        public int? Field3NumberValue { get; set; }
        public DateTime Field3DateTimeValue { get; set; }
        public string Field4StringValue { get; set; }
        public int? Field4NumberValue { get; set; }
        public DateTime Field4DateTimeValue { get; set; }
        public string Field5StringValue { get; set; }
        public int? Field5NumberValue { get; set; }
        public DateTime Field5DateTimeValue { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
