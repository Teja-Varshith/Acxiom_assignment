using axciom.Data;
using axciom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axciom.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string reportType = "Pipeline")
        {
            ViewBag.ReportType = reportType;
            var currentUser = await _userManager.GetUserAsync(User);
            var isSalesExec = await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive");

            switch (reportType)
            {
                case "Customer":
                    var custQuery = _context.Customers.Include(c => c.Owner).AsQueryable();
                    if (isSalesExec) custQuery = custQuery.Where(c => c.OwnerId == currentUser!.Id);
                    ViewBag.Data = await custQuery.OrderByDescending(c => c.CreatedDate).ToListAsync();
                    break;

                case "Lead":
                    var leadQuery = _context.Leads.Include(l => l.Owner).AsQueryable();
                    if (isSalesExec) leadQuery = leadQuery.Where(l => l.OwnerId == currentUser!.Id);
                    ViewBag.Data = await leadQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
                    break;

                case "FollowUp":
                    var followQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsQueryable();
                    if (isSalesExec) followQuery = followQuery.Where(f => f.AssignedTo == currentUser!.Id);
                    ViewBag.Data = await followQuery.OrderBy(f => f.FollowUpDate).ToListAsync();
                    break;

                case "Opportunity":
                    var oppQuery = _context.Opportunities.Include(o => o.Customer).Include(o => o.Owner).AsQueryable();
                    if (isSalesExec) oppQuery = oppQuery.Where(o => o.OwnerId == currentUser!.Id);
                    ViewBag.Data = await oppQuery.OrderByDescending(o => o.CreatedDate).ToListAsync();
                    break;

                case "Conversion":
                    var allLeads = await _context.Leads.ToListAsync();
                    var convertedCount = allLeads.Count(l => l.Status == "Converted");
                    var lostCount = allLeads.Count(l => l.Status == "Lost");
                    var inProgressCount = allLeads.Count(l => l.Status != "Converted" && l.Status != "Lost");

                    ViewBag.ConvertedCount = convertedCount;
                    ViewBag.LostCount = lostCount;
                    ViewBag.InProgressCount = inProgressCount;
                    ViewBag.TotalLeads = allLeads.Count;
                    ViewBag.ConversionRate = allLeads.Count > 0 ? (double)convertedCount / allLeads.Count * 100 : 0;
                    break;

                case "Pipeline":
                default:
                    var pipelineOppQuery = _context.Opportunities.Include(o => o.Customer).Include(o => o.Owner).AsQueryable();
                    if (isSalesExec) pipelineOppQuery = pipelineOppQuery.Where(o => o.OwnerId == currentUser!.Id);
                    var oppList = await pipelineOppQuery.ToListAsync();

                    ViewBag.TotalPipeline = oppList.Where(o => o.Status == "Active").Sum(o => o.Amount);
                    ViewBag.WeightedPipeline = oppList.Where(o => o.Status == "Active").Sum(o => o.WeightedPipeline);
                    ViewBag.Data = oppList;
                    break;
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(string reportType = "Pipeline")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isSalesExec = await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive");
            var builder = new StringBuilder();

            if (reportType == "Customer")
            {
                builder.AppendLine("CustomerCode,CustomerName,Email,Phone,CompanyName,Status,Owner,CreatedDate");
                var query = _context.Customers.Include(c => c.Owner).AsQueryable();
                if (isSalesExec) query = query.Where(c => c.OwnerId == currentUser!.Id);

                foreach (var item in await query.ToListAsync())
                {
                    builder.AppendLine($"\"{item.CustomerCode}\",\"{item.CustomerName}\",\"{item.Email}\",\"{item.Phone}\",\"{item.CompanyName}\",\"{item.Status}\",\"{item.Owner?.FullName}\",\"{item.CreatedDate:yyyy-MM-dd}\"");
                }
            }
            else if (reportType == "Lead")
            {
                builder.AppendLine("LeadCode,LeadName,Email,Phone,Source,Status,Priority,ExpectedValue,Owner,CreatedDate");
                var query = _context.Leads.Include(l => l.Owner).AsQueryable();
                if (isSalesExec) query = query.Where(l => l.OwnerId == currentUser!.Id);

                foreach (var item in await query.ToListAsync())
                {
                    builder.AppendLine($"\"{item.LeadCode}\",\"{item.LeadName}\",\"{item.Email}\",\"{item.Phone}\",\"{item.Source}\",\"{item.Status}\",\"{item.Priority}\",\"{item.ExpectedValue}\",\"{item.Owner?.FullName}\",\"{item.CreatedDate:yyyy-MM-dd}\"");
                }
            }
            else
            {
                builder.AppendLine("OpportunityName,Customer,Stage,Amount,Probability,WeightedPipeline,ExpectedCloseDate,Owner,Status");
                var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.Owner).AsQueryable();
                if (isSalesExec) query = query.Where(o => o.OwnerId == currentUser!.Id);

                foreach (var item in await query.ToListAsync())
                {
                    builder.AppendLine($"\"{item.OpportunityName}\",\"{item.Customer?.CustomerName}\",\"{item.Stage}\",\"{item.Amount}\",\"{item.Probability}\",\"{item.WeightedPipeline}\",\"{item.ExpectedCloseDate:yyyy-MM-dd}\",\"{item.Owner?.FullName}\",\"{item.Status}\"");
                }
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"{reportType}_Report_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
