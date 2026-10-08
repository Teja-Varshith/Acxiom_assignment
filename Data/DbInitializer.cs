using axciom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace axciom.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            // Seed Roles
            string[] roles = new[] { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            var adminEmail = "admin@acxiom.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Manager User
            var mgrEmail = "manager@acxiom.com";
            var mgrUser = await userManager.FindByEmailAsync(mgrEmail);
            if (mgrUser == null)
            {
                mgrUser = new ApplicationUser
                {
                    UserName = mgrEmail,
                    Email = mgrEmail,
                    FullName = "Sales Manager",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(mgrUser, "Manager@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(mgrUser, "Manager");
                }
            }

            // Seed Sales Executive User
            var salesEmail = "sales@acxiom.com";
            var salesUser = await userManager.FindByEmailAsync(salesEmail);
            if (salesUser == null)
            {
                salesUser = new ApplicationUser
                {
                    UserName = salesEmail,
                    Email = salesEmail,
                    FullName = "Sales Representative",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser, "Sales@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser, "SalesExecutive");
                }
            }

            // Seed Initial CRM Data if empty
            if (!context.Customers.Any())
            {
                var cust1 = new Customer
                {
                    CustomerCode = "CUST-1001",
                    CustomerName = "Acme Global Solutions",
                    Email = "contact@acmeglobal.com",
                    Phone = "9876543210",
                    CompanyName = "Acme Inc.",
                    Address = "123 Tech Park, Phase 1",
                    City = "Bangalore",
                    State = "Karnataka",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    OwnerId = salesUser?.Id
                };
                var cust2 = new Customer
                {
                    CustomerCode = "CUST-1002",
                    CustomerName = "Apex Financial Technologies",
                    Email = "info@apexfintech.com",
                    Phone = "9123456789",
                    CompanyName = "Apex Financial",
                    Address = "456 Finance Hub, BKC",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-15),
                    OwnerId = mgrUser?.Id
                };
                context.Customers.AddRange(cust1, cust2);
                await context.SaveChangesAsync();

                // Seed Leads
                var lead1 = new Lead
                {
                    LeadCode = "LEAD-2001",
                    LeadName = "Zenith Enterprises",
                    Email = "sales@zenithent.com",
                    Phone = "9988776655",
                    CompanyName = "Zenith",
                    Source = "Web",
                    Status = "Qualified",
                    Priority = "High",
                    ExpectedValue = 150000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    OwnerId = salesUser?.Id
                };
                var lead2 = new Lead
                {
                    LeadCode = "LEAD-2002",
                    LeadName = "Nexus Logistics",
                    Email = "inquiry@nexuslogistics.com",
                    Phone = "9811223344",
                    CompanyName = "Nexus Logistics",
                    Source = "Referral",
                    Status = "Contacted",
                    Priority = "Medium",
                    ExpectedValue = 75000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    OwnerId = salesUser?.Id
                };
                context.Leads.AddRange(lead1, lead2);
                await context.SaveChangesAsync();

                // Seed Opportunities
                var opp1 = new Opportunity
                {
                    OpportunityName = "Acme Cloud Migration Deal",
                    CustomerId = cust1.CustomerId,
                    Amount = 250000m,
                    Stage = "Proposal",
                    Probability = 70,
                    ExpectedCloseDate = DateTime.Today.AddDays(20),
                    Status = "Active",
                    OwnerId = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-12),
                    Notes = "Enterprise proposal submitted for 3-year cloud agreement."
                };
                var opp2 = new Opportunity
                {
                    OpportunityName = "Apex Security Audit Subscription",
                    CustomerId = cust2.CustomerId,
                    Amount = 180000m,
                    Stage = "Negotiation",
                    Probability = 85,
                    ExpectedCloseDate = DateTime.Today.AddDays(10),
                    Status = "Active",
                    OwnerId = mgrUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-8),
                    Notes = "Final contract review in progress with legal team."
                };
                context.Opportunities.AddRange(opp1, opp2);
                await context.SaveChangesAsync();

                // Seed FollowUps
                var follow1 = new FollowUp
                {
                    CustomerId = cust1.CustomerId,
                    RelatedType = "Customer",
                    RelatedId = cust1.CustomerId,
                    FollowUpDate = DateTime.Today.AddDays(2),
                    Subject = "Follow up on proposal terms",
                    FollowUpType = "Meeting",
                    Status = "Planned",
                    Remarks = "Schedule video call with CTO.",
                    AssignedTo = salesUser?.Id
                };
                var follow2 = new FollowUp
                {
                    LeadId = lead1.LeadId,
                    RelatedType = "Lead",
                    RelatedId = lead1.LeadId,
                    FollowUpDate = DateTime.Today.AddDays(1),
                    Subject = "Initial discovery call",
                    FollowUpType = "Call",
                    Status = "Planned",
                    Remarks = "Confirm software requirements.",
                    AssignedTo = salesUser?.Id
                };
                context.FollowUps.AddRange(follow1, follow2);
                await context.SaveChangesAsync();
            }
        }
    }
}
