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
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public CustomersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: Customers
        public async Task<IActionResult> Index(string searchTerm, string statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var query = _context.Customers.Include(c => c.Owner).AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(c => c.OwnerId == currentUser.Id);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CustomerName.Contains(searchTerm) ||
                                         c.Email.Contains(searchTerm) ||
                                         c.Phone.Contains(searchTerm) ||
                                         c.CompanyName.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;

            var customers = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null) return NotFound();

            // Authorization check
            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && customer.OwnerId != currentUser!.Id)
            {
                return Forbid();
            }

            // Get customer history (opportunities, followups)
            ViewBag.Opportunities = await _context.Opportunities.Where(o => o.CustomerId == id).ToListAsync();
            ViewBag.FollowUps = await _context.FollowUps.Where(f => f.CustomerId == id).ToListAsync();

            return View(customer);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            await PopulateOwnersDropDownList();
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Server-side Uniqueness Checks
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == customer.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "A customer with this email address already exists.");
            }

            if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
            }

            if (ModelState.IsValid)
            {
                customer.CustomerCode = "CUST-" + DateTime.UtcNow.Ticks.ToString()[^6..];
                customer.CreatedDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(customer.OwnerId))
                {
                    customer.OwnerId = currentUser?.Id;
                }

                _context.Add(customer);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "CreateCustomer", "Customer", customer.CustomerId.ToString(), "", customer.CustomerName, "Success", $"Created Customer {customer.CustomerCode}");

                TempData["SuccessMessage"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateOwnersDropDownList(customer.OwnerId);
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && customer.OwnerId != currentUser!.Id)
            {
                return Forbid();
            }

            await PopulateOwnersDropDownList(customer.OwnerId);
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.CustomerId) return NotFound();

            var existing = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (existing == null) return NotFound();

            // Uniqueness validation excluding current record
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == customer.Email.ToLower() && c.CustomerId != id))
            {
                ModelState.AddModelError("Email", "Another customer with this email address already exists.");
            }

            if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone && c.CustomerId != id))
            {
                ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
            }

            if (ModelState.IsValid)
            {
                customer.ModifiedDate = DateTime.UtcNow;
                customer.CustomerCode = existing.CustomerCode;
                customer.CreatedDate = existing.CreatedDate;

                _context.Update(customer);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "UpdateCustomer", "Customer", customer.CustomerId.ToString(), existing.CustomerName, customer.CustomerName, "Success", $"Updated Customer {customer.CustomerCode}");

                TempData["SuccessMessage"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateOwnersDropDownList(customer.OwnerId);
            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "DeleteCustomer", "Customer", id.ToString(), customer.CustomerName, "", "Success", $"Deleted Customer {customer.CustomerCode}");

                TempData["SuccessMessage"] = "Customer deleted successfully!";
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
