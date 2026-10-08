using axciom.Data;
using axciom.Models;
using axciom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace axciom.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public FollowUpsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: FollowUps
        public async Task<IActionResult> Index(string searchTerm, string statusFilter, string typeFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(f => f.AssignedTo == currentUser.Id);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(f => f.Subject.Contains(searchTerm) ||
                                         (f.Customer != null && f.Customer.CustomerName.Contains(searchTerm)) ||
                                         (f.Lead != null && f.Lead.LeadName.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(f => f.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(f => f.FollowUpType == typeFilter);
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.TypeFilter = typeFilter;

            var followups = await query.OrderBy(f => f.FollowUpDate).ToListAsync();
            return View(followups);
        }

        // GET: FollowUps/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            var followUp = new FollowUp
            {
                FollowUpDate = DateTime.Today,
                Status = "Planned"
            };
            return View(followUp);
        }

        // POST: FollowUps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUp followUp)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Business Validation Rule: Date cannot be earlier than today for planned followups
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (ModelState.IsValid)
            {
                followUp.CreatedDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(followUp.AssignedTo))
                {
                    followUp.AssignedTo = currentUser?.Id;
                }

                _context.Add(followUp);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "CreateFollowUp", "FollowUp", followUp.FollowUpId.ToString(), "", followUp.Subject, "Success", $"Scheduled Follow-Up for {followUp.FollowUpDate:yyyy-MM-dd}");

                TempData["SuccessMessage"] = "Follow-up scheduled successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
            return View(followUp);
        }

        // GET: FollowUps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && followUp.AssignedTo != currentUser!.Id)
            {
                return Forbid();
            }

            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
            return View(followUp);
        }

        // POST: FollowUps/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FollowUp followUp)
        {
            if (id != followUp.FollowUpId) return NotFound();

            var existing = await _context.FollowUps.AsNoTracking().FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (existing == null) return NotFound();

            // Business Validation Rule
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (ModelState.IsValid)
            {
                followUp.CreatedDate = existing.CreatedDate;

                _context.Update(followUp);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "UpdateFollowUp", "FollowUp", followUp.FollowUpId.ToString(), existing.Status, followUp.Status, "Success", $"Updated Follow-Up status to {followUp.Status}");

                TempData["SuccessMessage"] = "Follow-up updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
            return View(followUp);
        }

        // POST: FollowUps/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string remarks)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp != null)
            {
                followUp.Status = "Completed";
                if (!string.IsNullOrEmpty(remarks))
                {
                    followUp.Remarks = (followUp.Remarks + " " + remarks).Trim();
                }
                _context.Update(followUp);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "CompleteFollowUp", "FollowUp", id.ToString(), "Planned", "Completed", "Success", $"Completed Follow-Up: {followUp.Subject}");

                TempData["SuccessMessage"] = "Follow-up marked as Completed!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUps/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp != null)
            {
                _context.FollowUps.Remove(followUp);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "DeleteFollowUp", "FollowUp", id.ToString(), followUp.Subject, "", "Success", $"Deleted Follow-Up: {followUp.Subject}");

                TempData["SuccessMessage"] = "Follow-up deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(object? selectedCustomer = null, object? selectedLead = null, object? selectedUser = null)
        {
            var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
            var leads = await _context.Leads.Where(l => l.Status != "Converted" && l.Status != "Lost").OrderBy(l => l.LeadName).ToListAsync();
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            ViewBag.CustomerId = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomer);
            ViewBag.LeadId = new SelectList(leads, "LeadId", "LeadName", selectedLead);
            ViewBag.AssignedTo = new SelectList(users, "Id", "FullName", selectedUser);
        }
    }
}
