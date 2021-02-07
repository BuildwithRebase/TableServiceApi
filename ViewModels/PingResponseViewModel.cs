using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public class PingResponseViewModel
    {
        public string Message { get; set; }
        public bool Authorized { get; set; }
    }
}
