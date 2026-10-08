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
    [Route("api/followups")]
    [Authorize]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public FollowUpsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: /api/followups
        [HttpGet]
        public async Task<IActionResult> GetFollowUps()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var query = _context.FollowUps.Include(f => f.AssignedUser).AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(f => f.AssignedTo == currentUser.Id);
            }

            var dtos = await query.Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                LeadId = f.LeadId,
                RelatedType = f.RelatedType,
                FollowUpDate = f.FollowUpDate,
                Subject = f.Subject,
                FollowUpType = f.FollowUpType,
                Status = f.Status,
                Remarks = f.Remarks,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : ""
            }).ToListAsync();

            return Ok(dtos);
        }

        // POST: /api/followups
        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] FollowUpDto dto)
        {
            if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                RelatedType = dto.RelatedType ?? "Customer",
                FollowUpDate = dto.FollowUpDate,
                Subject = dto.Subject,
                FollowUpType = dto.FollowUpType ?? "Call",
                Status = dto.Status ?? "Planned",
                Remarks = dto.Remarks ?? "",
                AssignedTo = currentUser?.Id,
                CreatedDate = DateTime.UtcNow
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ApiCreateFollowUp", "FollowUp", followUp.FollowUpId.ToString(), "", followUp.Subject);

            dto.FollowUpId = followUp.FollowUpId;
            return CreatedAtAction(nameof(GetFollowUps), new { id = followUp.FollowUpId }, dto);
        }
    }
}
