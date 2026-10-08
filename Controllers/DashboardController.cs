using axciom.Data;
using axciom.Models;
using axciom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace axciom.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string dateRange = "ThisMonth")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var isSalesExec = await _userManager.IsInRoleAsync(currentUser, "SalesExecutive");
            var isManager = await _userManager.IsInRoleAsync(currentUser, "Manager");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Base Queries scoped by role
            IQueryable<Customer> customerQuery = _context.Customers;
            IQueryable<Lead> leadQuery = _context.Leads;
            IQueryable<Opportunity> oppQuery = _context.Opportunities;

            if (isSalesExec)
            {
                customerQuery = customerQuery.Where(c => c.OwnerId == currentUser.Id);
                leadQuery = leadQuery.Where(l => l.OwnerId == currentUser.Id);
                oppQuery = oppQuery.Where(o => o.OwnerId == currentUser.Id);
            }

            // Apply Date Range Filter if requested
            DateTime startDate = DateTime.MinValue;
            var now = DateTime.UtcNow;

            switch (dateRange)
            {
                case "Today":
                    startDate = DateTime.Today;
                    break;
                case "ThisWeek":
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case "ThisMonth":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
            }

            if (startDate != DateTime.MinValue)
            {
                customerQuery = customerQuery.Where(c => c.CreatedDate >= startDate);
                leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate);
                oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate);
            }

            var activeOppAmounts = await oppQuery.Where(o => o.Status == "Active" && o.Stage != "Won" && o.Stage != "Lost")
                                                 .Select(o => o.Amount)
                                                 .ToListAsync();

            var vm = new DashboardViewModel
            {
                DateRangeFilter = dateRange,
                TotalCustomers = await customerQuery.CountAsync(),
                TotalLeads = await leadQuery.CountAsync(),
                OpenLeads = await leadQuery.Where(l => l.Status != "Converted" && l.Status != "Lost").CountAsync(),
                TotalOpportunities = await oppQuery.CountAsync(),
                OpenOpportunities = await oppQuery.Where(o => o.Status == "Active" && o.Stage != "Won" && o.Stage != "Lost").CountAsync(),
                WonOpportunities = await oppQuery.Where(o => o.Stage == "Won").CountAsync(),
                LostOpportunities = await oppQuery.Where(o => o.Stage == "Lost").CountAsync(),
                TotalPipelineValue = activeOppAmounts.Sum()
            };

            // Chart 1: Lead Status Breakdown
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
            foreach (var status in leadStatuses)
            {
                vm.LeadStatusLabels.Add(status);
                vm.LeadStatusCounts.Add(await leadQuery.CountAsync(l => l.Status == status));
            }

            // Chart 2: Opportunity Pipeline Stage
            var pipelineStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            foreach (var stage in pipelineStages)
            {
                vm.PipelineStageLabels.Add(stage);
                var stageAmounts = await oppQuery.Where(o => o.Stage == stage).Select(o => o.Amount).ToListAsync();
                vm.PipelineStageAmounts.Add(stageAmounts.Sum());
            }

            // Chart 3: Monthly Sales (Last 6 Months)
            for (int i = 5; i >= 0; i--)
            {
                var mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var mEnd = mStart.AddMonths(1);
                var mLabel = mStart.ToString("MMM yyyy");
                var monthAmounts = await oppQuery.Where(o => o.Stage == "Won" && o.CreatedDate >= mStart && o.CreatedDate < mEnd)
                                                .Select(o => o.Amount)
                                                .ToListAsync();

                vm.MonthlySalesLabels.Add(mLabel);
                vm.MonthlySalesValues.Add(monthAmounts.Sum());
            }

            return View(vm);
        }
    }
}
