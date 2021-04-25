using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableService.Core.Models
{
    public record ApiSession(string Email, string FullName, int TeamId, string TeamName, string TablePrefix, bool IsSubscriber, bool IsSuperAdmin, bool IsAdmin);
}
