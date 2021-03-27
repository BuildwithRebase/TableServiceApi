using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableServiceApi.Messages
{
    public record AddTeamMessage(string TeamName, string ContactUserName, string ContactUserEmail, string BillFlowSecret);
}
