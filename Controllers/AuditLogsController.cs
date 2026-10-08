using axciom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace axciom.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm, string entityFilter, string actionFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a => a.UserName.Contains(searchTerm) ||
                                         a.Details.Contains(searchTerm) ||
                                         a.RecordId.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(entityFilter))
            {
                query = query.Where(a => a.EntityName == entityFilter);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.EntityFilter = entityFilter;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var logs = await query.OrderByDescending(a => a.Timestamp).Take(200).ToListAsync();
            return View(logs);
        }
    }
}
