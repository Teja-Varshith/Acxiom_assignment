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
    [Route("api/opportunities")]
    [Authorize]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public OpportunitiesApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: /api/opportunities
        [HttpGet]
        public async Task<IActionResult> GetOpportunities()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.Owner).AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(o => o.OwnerId == currentUser.Id);
            }

            var dtos = await query.Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "",
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                OwnerName = o.Owner != null ? o.Owner.FullName : ""
            }).ToListAsync();

            return Ok(dtos);
        }

        // POST: /api/opportunities
        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] OpportunityDto dto)
        {
            // Business Validation
            if (dto.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (dto.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                Stage = dto.Stage ?? "Qualification",
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = dto.Status ?? "Active",
                CreatedDate = DateTime.UtcNow,
                OwnerId = currentUser?.Id
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ApiCreateOpportunity", "Opportunity", opp.OpportunityId.ToString(), "", opp.OpportunityName);

            dto.OpportunityId = opp.OpportunityId;
            dto.CreatedDate = opp.CreatedDate;

            return CreatedAtAction(nameof(GetOpportunities), new { id = opp.OpportunityId }, dto);
        }
    }
}
