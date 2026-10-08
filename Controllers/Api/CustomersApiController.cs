using axciom.Data;
using axciom.DTOs;
using axciom.Models;
using axciom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace axciom.Controllers.Api
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public CustomersApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: /api/customers
        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var query = _context.Customers.Include(c => c.Owner).AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(c => c.OwnerId == currentUser.Id);
            }

            var dtos = await query.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                OwnerName = c.Owner != null ? c.Owner.FullName : ""
            }).ToListAsync();

            return Ok(dtos);
        }

        // GET: /api/customers/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var c = await _context.Customers.Include(x => x.Owner).FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c == null) return NotFound(new { message = "Customer not found." });

            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && c.OwnerId != currentUser!.Id)
            {
                return StatusCode(403, new { message = "Access denied to this customer record." });
            }

            var dto = new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                OwnerName = c.Owner?.FullName ?? ""
            };

            return Ok(dto);
        }

        // POST: /api/customers
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
            {
                return StatusCode(409, new { message = "Customer with this email address already exists." });
            }

            if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
            {
                return StatusCode(409, new { message = "Customer with this phone number already exists." });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var customer = new Customer
            {
                CustomerCode = "CUST-" + DateTime.UtcNow.Ticks.ToString()[^6..],
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName ?? "",
                Address = dto.Address ?? "",
                City = dto.City ?? "",
                State = dto.State ?? "",
                Status = dto.Status ?? "Active",
                CreatedDate = DateTime.UtcNow,
                OwnerId = currentUser?.Id
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ApiCreateCustomer", "Customer", customer.CustomerId.ToString(), "", customer.CustomerName);

            dto.CustomerId = customer.CustomerId;
            dto.CustomerCode = customer.CustomerCode;
            dto.CreatedDate = customer.CreatedDate;

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, dto);
        }

        // PUT: /api/customers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && customer.OwnerId != currentUser!.Id)
            {
                return StatusCode(403, new { message = "Access denied." });
            }

            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower() && c.CustomerId != id))
            {
                return StatusCode(409, new { message = "Email address is used by another customer." });
            }

            if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone && c.CustomerId != id))
            {
                return StatusCode(409, new { message = "Phone number is used by another customer." });
            }

            customer.CustomerName = dto.CustomerName;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.CompanyName = dto.CompanyName ?? customer.CompanyName;
            customer.Address = dto.Address ?? customer.Address;
            customer.City = dto.City ?? customer.City;
            customer.State = dto.State ?? customer.State;
            customer.Status = dto.Status ?? customer.Status;
            customer.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ApiUpdateCustomer", "Customer", id.ToString());

            return Ok(new { message = "Customer updated successfully." });
        }

        // DELETE: /api/customers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            var currentUser = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(currentUser!, "SalesExecutive") && customer.OwnerId != currentUser!.Id)
            {
                return StatusCode(403, new { message = "Access denied." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ApiDeleteCustomer", "Customer", id.ToString());
            return Ok(new { message = "Customer deleted successfully." });
        }
    }
}
