using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.Messages
{
    public record EditUserMessage (int Id, string FirstName, string LastName);
}
