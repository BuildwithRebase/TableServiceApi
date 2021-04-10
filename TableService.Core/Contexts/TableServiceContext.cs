using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using TableService.Core.Models;
using TableService.Core.Utility;


namespace TableService.Core.Contexts
{
    public class TableServiceContext : DbContext
    {
        // to-do put this in the app.settings
        public const string ConnectionString = "";

        public DbSet<Team> Teams { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<ApiSession> ApiSessions { get; set; }
//        public DbSet<Plan> Plans { get; set; }
//        public DbSet<Sale> Sales { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseSqlite("Data Source=tableservice.db");
            optionsBuilder.UseMySQL(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Team defaultTeam = CreateTeam(1, null, "Build with rebase", true);
            Team childTeam = CreateTeam(2, 1, "Test client", false);

            User defaultUser = CreateUser(1, "muncey", "philip.munce@gmail.com", PasswordUtility.HashPassword("ZfmoU98M"), "Philip", "Munce", true, true, 1, "Build with rebase");
            User michaelUser = CreateUser(3, "michael", "Ali3nS4n@gmail.com", PasswordUtility.HashPassword("HpvpBz7H"), "Michael", "Rapson", true, true, 1, "Build with rebase");
            User bryanUser = CreateUser(4, "bryan", "bzavestoski@gmail.com", PasswordUtility.HashPassword("muYFEB2R"), "Bryan", "Zavestoski", true, true, 1, "Build with rebase");


            User generalUser = CreateUser(2, "generaluser", "philip.munce@munceyweb.com", PasswordUtility.HashPassword("password123"), "General", "User", false, false, 2, "Test client");

//            Table tasksTable = CreateTable(1, 1, "Build with rebase", "Task", "Tasks", "TaskName,TaskStatus,AssignedTo,DueDate,Comments", "string,string,string,datetime,string");

            modelBuilder.Entity<Team>().HasData(defaultTeam, childTeam);
            modelBuilder.Entity<User>().HasData(defaultUser, generalUser, michaelUser, bryanUser);
//          modelBuilder.Entity<Table>().HasData(tasksTable);

  /*          modelBuilder.Entity<Plan>().HasData(
                CreatePlan(1, 1, "Build with rebase", 1, "Free", "-", "100", "0", "0"),
                CreatePlan(2, 1, "Build with rebase", 2, "Premium", "Monthly", "1000", "10", "100")
            );

            modelBuilder.Entity<Sale>().HasData(
                CreateSale(1, 1, "Build with rebase", "John smith", 1, "100", "10 May 2020", "Premium"),
                CreateSale(2, 1, "Build with rebase", "John smith", 1, "100", "10 May 2020", "Premium"),
                CreateSale(3, 1, "Build with rebase", "John smith", 1, "100", "10 May 2020", "Premium"));*/
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

        private Table CreateTable(int id, int teamId, string teamName, string tableName, string tableLabel, string fieldNames, string fieldTypes)
        {
            return new Table
            {
                Id = id,
                TeamId = teamId,
                TeamName = teamName,
                TableName = tableName,
                TableLabel = tableLabel,
                FieldNames = fieldNames,
                FieldTypes = fieldTypes,
                CreatedAt = DateTime.Now,
                CreatedUserName = "muncey",
                UpdatedAt = DateTime.Now,
                UpdatedUserName = "muncey"
            };
        }

        private Plan CreatePlan(int id, int teamId, string teamName, int displayOrder, string planName, string billingFrequency, string userCount, string monthlyCost, string annualCost)
        {
            return new Plan
            {
                Id = id,
                TeamId = teamId,
                TeamName = teamName,
                DisplayOrder = displayOrder,
                PlanName = planName,
                BillingFrequency = billingFrequency,
                UserCount = userCount,
                MonthlyCost = monthlyCost,
                AnnualCost = annualCost,
                CreatedAt = DateTime.Now,
                CreatedUserName = "muncey",
                UpdatedAt = DateTime.Now,
                UpdatedUserName = "muncey"
            };
        }

        private Sale CreateSale(int id, int teamId, string teamName, string customerName, int planId, string saleAmount, string saleDate, string planName)
        {
            return new Sale
            {
                Id = id,
                TeamId = teamId,
                TeamName = teamName,
                CustomerName = customerName,
                PlanId = planId,
                SaleAmount = saleAmount,
                SaleDate = saleDate,
                PlanName = planName,
                CreatedAt = DateTime.Now,
                CreatedUserName = "muncey",
                UpdatedAt = DateTime.Now,
                UpdatedUserName = "muncey"
            };
        }
        
    }
}
