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
        public DbSet<Table> Tables { get; set; }
        public DbSet<TableRecord> TableRecords { get; set; }
        public DbSet<ApiSession> ApiSessions { get; set; }

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

            Table tasksTable = CreateTable(1, 1, "Build with rebase", "tasks", "Tasks", "task_name", "task_status", "assigned_to", "due_date", "comments");

            modelBuilder.Entity<Team>().HasData(defaultTeam, childTeam);
            modelBuilder.Entity<User>().HasData(defaultUser, generalUser);
            modelBuilder.Entity<Table>().HasData(tasksTable);
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

        private Table CreateTable(int id, int teamId, string teamName, string tableName, string tableLabel, string field1, string field2, string field3, string field4, string field5)
        {
            return new Table
            {
                Id = id,
                TeamId = teamId,
                TeamName = teamName,
                TableName = tableName,
                TableLabel = tableLabel,
                Field1Name = field1,
                Field1Type = "string",
                Field2Name = field2,
                Field2Type = "string",
                Field3Name = field3,
                Field3Type = "string",
                Field4Name = field4,
                Field4Type = "string",
                Field5Name = field5,
                Field5Type = "string",
                CreatedAt = DateTime.Now,
                CreatedUserName = "muncey",
                UpdatedAt = DateTime.Now,
                UpdatedUserName = "muncey"
            };
        }
        
    }
}
