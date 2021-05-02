using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableService.Core.Messages
{
    public record SubscriberAddRequest(string Email, string FirstName, string LastName, string Password, string ConfirmPassword);
}
