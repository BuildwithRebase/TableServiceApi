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
        public string TableLabel { get; set; }
        public string Field1Name { get; set; }
        public string Field1Type { get; set; }
        public string Field2Name { get; set; }
        public string Field2Type { get; set; }
        public string Field3Name { get; set; }
        public string Field3Type { get; set; }
        public string Field4Name { get; set; }
        public string Field4Type { get; set; }
        public string Field5Name { get; set; }
        public string Field5Type { get; set; }
        public string CreatedUserName { get; set; }
        public string UpdatedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
