using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Enums;

namespace RewardsAndRecognitionRepository.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsersAsync(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            // Step 1: Define roles
            string[] roleNames = { "Admin", "TeamLead", "Manager", "Director", "Employee" };

            foreach (var role in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
        public static async Task SeedAdminAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. Ensure the Admin role exists
            var adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            // 2. Create the admin user if it doesn't exist
            var adminEmail = "admin@rnr.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Name = "Admin",
                    SelectedRole = "Admin"
                };
                // Set a strong password here
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                }
            }
        }

        public static async Task SeedTestDataAsync(ApplicationDbContext context)
        {
            // Seed YearQuarter
            if (!context.YearQuarters.Any())
            {
                context.YearQuarters.Add(new YearQuarter
                {
                    Year = 2026,
                    Quarter = Enums.Quarter.Q1,
                    IsActive = true,
                    IsDeleted = false
                });
                await context.SaveChangesAsync();
            }

            // Seed Categories
            if (!context.Categories.Any())
            {
                context.Categories.Add(new Category
                {
                    Name = "Innovation",
                    Description = "For innovative ideas",
                    isActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                });
                context.Categories.Add(new Category
                {
                    Name = "Teamwork",
                    Description = "For team collaboration",
                    isActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // Seed a test user
            if (!context.Users.Any())
            {
                var testUser = new User
                {
                    UserName = "test@rnr.com",
                    Email = "test@rnr.com",
                    EmailConfirmed = true,
                    Name = "Test User",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(testUser);
                await context.SaveChangesAsync();
            }
        }
    }
}
