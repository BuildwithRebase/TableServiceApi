﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Types;

namespace TableServiceApi.ViewModels
{
    public class TeamTable
    {
        public string TableName { get; set; }
        public string TableLabel { get; set; }
        public int Id { get; set; }
        public string FieldNames { get; set; }
        public string FieldTypes { get; set; }
        public TableStateType TableState { get; set;  }
    }
}
