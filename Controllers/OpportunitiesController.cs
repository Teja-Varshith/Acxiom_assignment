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
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public OpportunitiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: Opportunities
        public async Task<IActionResult> Index(string searchTerm, string stageFilter, string statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.Owner)
                .AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(o => o.OwnerId == currentUser.Id);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o => o.OpportunityName.Contains(searchTerm) ||
                                         (o.Customer != null && o.Customer.CustomerName.Contains(searchTerm)) ||
                                         (o.Lead != null && o.Lead.LeadName.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(stageFilter))
            {
                query = query.Where(o => o.Stage == stageFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StageFilter = stageFilter;
            ViewBag.StatusFilter = statusFilter;

            var opportunities = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
            return View(opportunities);
        }

        // GET: Opportunities/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            var opp = new Opportunity
            {
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                Probability = 50,
                Amount = 10000m
            };
            return View(opp);
        }

        // POST: Opportunities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Opportunity opportunity)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Mandatory Business Validation Rules
            if (opportunity.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (opportunity.Status == "Active" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                opportunity.CreatedDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(opportunity.OwnerId))
                {
                    opportunity.OwnerId = currentUser?.Id;
                }

                _context.Add(opportunity);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "CreateOpportunity", "Opportunity", opportunity.OpportunityId.ToString(), "", opportunity.OpportunityName, "Success", $"Created Opportunity '{opportunity.OpportunityName}' with Amount {opportunity.Amount:C}");

                TempData["SuccessMessage"] = "Opportunity created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.OwnerId);
            return View(opportunity);
        }

        // GET: Opportunities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && opportunity.OwnerId != currentUser!.Id)
            {
                return Forbid();
            }

            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.OwnerId);
            return View(opportunity);
        }

        // POST: Opportunities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Opportunity opportunity)
        {
            if (id != opportunity.OpportunityId) return NotFound();

            var existing = await _context.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (existing == null) return NotFound();

            // Mandatory Business Validation Rules
            if (opportunity.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (opportunity.Status == "Active" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                opportunity.CreatedDate = existing.CreatedDate;

                _context.Update(opportunity);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "UpdateOpportunity", "Opportunity", opportunity.OpportunityId.ToString(), existing.Stage, opportunity.Stage, "Success", $"Updated Opportunity '{opportunity.OpportunityName}'");

                TempData["SuccessMessage"] = "Opportunity updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.OwnerId);
            return View(opportunity);
        }

        // POST: Opportunities/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity != null)
            {
                _context.Opportunities.Remove(opportunity);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "DeleteOpportunity", "Opportunity", id.ToString(), opportunity.OpportunityName, "", "Success", $"Deleted Opportunity '{opportunity.OpportunityName}'");

                TempData["SuccessMessage"] = "Opportunity deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(object? selectedCustomer = null, object? selectedLead = null, object? selectedOwner = null)
        {
            var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
            var leads = await _context.Leads.Where(l => l.Status != "Converted" && l.Status != "Lost").OrderBy(l => l.LeadName).ToListAsync();
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            ViewBag.CustomerId = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomer);
            ViewBag.LeadId = new SelectList(leads, "LeadId", "LeadName", selectedLead);
            ViewBag.OwnerId = new SelectList(users, "Id", "FullName", selectedOwner);
        }
    }
}
