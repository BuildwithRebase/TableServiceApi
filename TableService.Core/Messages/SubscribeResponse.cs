using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableService.Core.Messages
{
    public record SubscribeResponse(int Id, string Email);
}
