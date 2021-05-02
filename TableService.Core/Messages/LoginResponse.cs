using System.Collections.Generic;
using TableServiceApi.ViewModels;

namespace TableService.Core.Messages
{
    public record LoginResponse(string Jwt, int Id, string Email, string FirstName, string LastName, int TeamId, string TeamName, List<TeamTable> Tables);
}
