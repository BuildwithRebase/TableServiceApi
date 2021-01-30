using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using TableService.Core.Models;
using TableService.Core.Utility;


namespace TableService.Core.Contexts
{
    public class TableServiceContext : DbContext
    {
        public DbSet<Team> Teams { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=tableservice.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Team defaultTeam = CreateTeam(1, null, "Build with rebase", true);
            Team childTeam = CreateTeam(2, 1, "Test client", false);

            User defaultUser = CreateUser(1, "muncey", "philip.munce@gmail.com", PasswordUtility.HashPassword("ZfmoU98M"), "Philip", "Munce", true, true, 1, "Build with rebase");
            User generalUser = CreateUser(2, "generaluser", "philip.munce@munceyweb.com", PasswordUtility.HashPassword("password123"), "General", "User", false, false, 2, "Test client");

            modelBuilder.Entity<Team>().HasData(defaultTeam, childTeam);
            modelBuilder.Entity<User>().HasData(defaultUser, generalUser);
        }

        private Team CreateTeam(int id, int? parentTeamId, string teamName, bool isAdmin)
        {
            return new Team
            {
                Id = id,
                ParentTeamId = parentTeamId,
                TeamName = teamName,
                ContactEmail = "test@email.com",
                IsAdmin = isAdmin,
                TablePrefix = TableUtility.GetTablePrefixFromName(teamName),
                CreatedAt = DateTime.Now,
                CreatedUserName = "test",
                UpdatedAt = DateTime.Now,
                UpdatedUserName = "test"
            };
        }

        private User CreateUser(int id, string userName, string email, string userPassword, string firstName, string lastName, bool isAdmin, bool isSuperAdmin, int teamId, string teamName)
        {
            return new User
            {
                Id = id,
                UserName = userName,
                Email = email,
                UserPassword = userPassword,
                FirstName = firstName,
                LastName = lastName,
                IsAdmin = isAdmin,
                IsSuperAdmin = isSuperAdmin,
                TeamId = teamId,
                TeamName = teamName,
                CreatedAt = DateTime.Now,
                CreatedUserName = "test",
                UpdatedAt = DateTime.Now,
                UpdatedUserName = "test"
            };
        }
    }
}
