using axciom.Data;
using axciom.Models;
using axciom.Services;
using axciom.ViewModels;
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
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public LeadsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: Leads
        public async Task<IActionResult> Index(string searchTerm, string statusFilter, string sourceFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var query = _context.Leads.Include(l => l.Owner).AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(l => l.OwnerId == currentUser.Id);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(l => l.LeadName.Contains(searchTerm) ||
                                         l.Email.Contains(searchTerm) ||
                                         l.Phone.Contains(searchTerm) ||
                                         l.CompanyName.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(l => l.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(sourceFilter))
            {
                query = query.Where(l => l.Source == sourceFilter);
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SourceFilter = sourceFilter;

            var leads = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
            return View(leads);
        }

        // GET: Leads/Create
        public async Task<IActionResult> Create()
        {
            await PopulateOwnersDropDownList();
            return View();
        }

        // POST: Leads/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lead lead)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                lead.LeadCode = "LEAD-" + DateTime.UtcNow.Ticks.ToString()[^6..];
                lead.CreatedDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(lead.OwnerId))
                {
                    lead.OwnerId = currentUser?.Id;
                }

                _context.Add(lead);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "CreateLead", "Lead", lead.LeadId.ToString(), "", lead.LeadName, "Success", $"Created Lead {lead.LeadCode}");

                TempData["SuccessMessage"] = "Lead created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateOwnersDropDownList(lead.OwnerId);
            return View(lead);
        }

        // GET: Leads/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && lead.OwnerId != currentUser!.Id)
            {
                return Forbid();
            }

            await PopulateOwnersDropDownList(lead.OwnerId);
            return View(lead);
        }

        // POST: Leads/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lead lead)
        {
            if (id != lead.LeadId) return NotFound();

            var existing = await _context.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == id);
            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                lead.LeadCode = existing.LeadCode;
                lead.CreatedDate = existing.CreatedDate;

                _context.Update(lead);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "UpdateLead", "Lead", lead.LeadId.ToString(), existing.Status, lead.Status, "Success", $"Updated Lead {lead.LeadCode}");

                TempData["SuccessMessage"] = "Lead updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateOwnersDropDownList(lead.OwnerId);
            return View(lead);
        }

        // GET: Leads/Convert/5
        public async Task<IActionResult> Convert(int id)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();

            var vm = new LeadConversionViewModel
            {
                LeadId = lead.LeadId,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = string.IsNullOrEmpty(lead.CompanyName) ? lead.LeadName : lead.CompanyName,
                CustomerName = string.IsNullOrEmpty(lead.CompanyName) ? lead.LeadName : lead.CompanyName,
                OpportunityName = $"{lead.LeadName} Deal",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 50000m,
                Stage = "Qualification",
                Probability = 50,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            return View(vm);
        }

        // POST: Leads/Convert/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConversionViewModel model)
        {
            var lead = await _context.Leads.FindAsync(model.LeadId);
            if (lead == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Customer? createdCustomer = null;

            // 1. Create Customer if checked
            if (model.CreateCustomer)
            {
                createdCustomer = new Customer
                {
                    CustomerCode = "CUST-" + DateTime.UtcNow.Ticks.ToString()[^6..],
                    CustomerName = model.CustomerName,
                    Email = model.Email,
                    Phone = model.Phone,
                    CompanyName = model.CompanyName,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    OwnerId = lead.OwnerId ?? currentUser?.Id
                };
                _context.Customers.Add(createdCustomer);
                await _context.SaveChangesAsync();
            }

            // 2. Create Opportunity if checked
            if (model.CreateOpportunity)
            {
                var opp = new Opportunity
                {
                    OpportunityName = model.OpportunityName,
                    CustomerId = createdCustomer?.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount,
                    Stage = model.Stage,
                    Probability = model.Probability,
                    ExpectedCloseDate = model.ExpectedCloseDate,
                    Status = "Active",
                    OwnerId = lead.OwnerId ?? currentUser?.Id,
                    CreatedDate = DateTime.UtcNow,
                    Notes = $"Converted from Lead {lead.LeadCode}"
                };
                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();
            }

            // 3. Mark Lead as Converted
            lead.Status = "Converted";
            _context.Update(lead);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ConvertLead", "Lead", lead.LeadId.ToString(), "Qualified", "Converted", "Success", $"Converted Lead {lead.LeadCode} into Customer/Opportunity");

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' successfully converted!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Leads/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead != null)
            {
                _context.Leads.Remove(lead);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "DeleteLead", "Lead", id.ToString(), lead.LeadName, "", "Success", $"Deleted Lead {lead.LeadCode}");

                TempData["SuccessMessage"] = "Lead deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateOwnersDropDownList(object? selectedOwner = null)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.OwnerId = new SelectList(users, "Id", "FullName", selectedOwner);
        }
    }
}
