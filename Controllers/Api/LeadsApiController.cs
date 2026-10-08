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
    [Route("api/leads")]
    [Authorize]
    public class LeadsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public LeadsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: /api/leads
        [HttpGet]
        public async Task<IActionResult> GetLeads()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var query = _context.Leads.Include(l => l.Owner).AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(l => l.OwnerId == currentUser.Id);
            }

            var dtos = await query.Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                OwnerName = l.Owner != null ? l.Owner.FullName : ""
            }).ToListAsync();

            return Ok(dtos);
        }

        // POST: /api/leads
        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] LeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var lead = new Lead
            {
                LeadCode = "LEAD-" + DateTime.UtcNow.Ticks.ToString()[^6..],
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName ?? "",
                Source = dto.Source ?? "Web",
                Status = dto.Status ?? "New",
                Priority = dto.Priority ?? "Medium",
                ExpectedValue = dto.ExpectedValue,
                CreatedDate = DateTime.UtcNow,
                OwnerId = currentUser?.Id
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ApiCreateLead", "Lead", lead.LeadId.ToString(), "", lead.LeadName);

            dto.LeadId = lead.LeadId;
            dto.LeadCode = lead.LeadCode;
            dto.CreatedDate = lead.CreatedDate;

            return CreatedAtAction(nameof(GetLeads), new { id = lead.LeadId }, dto);
        }
    }
}
