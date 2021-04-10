using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TableService.Core.Models;

namespace TableServiceApi.TableService.Core.Contexts
{
    public class TeamDbContext : DbContext
    {
        protected IHttpContextAccessor HttpContextAccessor { get; }
        public DbSet<TableRecord> TableRecords { get; set; }

        public TeamDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            this.HttpContextAccessor = httpContextAccessor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Set the Db name base on the team name
            var apiSession = (ApiSession) HttpContextAccessor.HttpContext.Items["api_session"];
            var dbName = FormatTeamNameAsDbName(apiSession.TeamName);

            // Use Sqlite (for now)
            optionsBuilder.UseSqlite("Data Source=" + dbName + ".db");

        }

        private string FormatTeamNameAsDbName(string teamName)
        {
            var result = teamName.Replace(" ", "").ToLower();
            if (result.Length <= 20)
            {
                return result;
            }
            return result.Substring(0, 20);
        }

    }
}
