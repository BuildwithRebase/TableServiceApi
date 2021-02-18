using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.ViewModels
{
    public class MessageViewModel
    {
        public MessageViewModel(string message)
        {
            this.Message = message;
        }
        public string Message { get; set; }
    }
}
