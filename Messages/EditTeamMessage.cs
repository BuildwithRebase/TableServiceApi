using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.Messages
{
    public record EditTeamMessage(int Id, string ContactUserName, string ContactEmail, string BillFlowSecret);
}
