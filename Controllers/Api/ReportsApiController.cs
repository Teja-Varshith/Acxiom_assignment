using axciom.Data;
using axciom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace axciom.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /api/reports/pipeline
        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.Owner).AsQueryable();

            if (await _userManager.IsInRoleAsync(currentUser, "SalesExecutive"))
            {
                query = query.Where(o => o.OwnerId == currentUser.Id);
            }

            var opps = await query.ToListAsync();

            var stageWise = opps.GroupBy(o => o.Stage).Select(g => new
            {
                Stage = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.Amount),
                WeightedAmount = g.Sum(o => o.WeightedPipeline)
            });

            var ownerWise = opps.GroupBy(o => o.Owner != null ? o.Owner.FullName : "Unassigned").Select(g => new
            {
                Owner = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.Amount),
                WeightedAmount = g.Sum(o => o.WeightedPipeline)
            });

            return Ok(new
            {
                totalPipelineAmount = opps.Where(o => o.Status == "Active").Sum(o => o.Amount),
                totalWeightedAmount = opps.Where(o => o.Status == "Active").Sum(o => o.WeightedPipeline),
                stageSummary = stageWise,
                ownerSummary = ownerWise,
                opportunities = opps.Select(o => new
                {
                    o.OpportunityId,
                    o.OpportunityName,
                    CustomerName = o.Customer?.CustomerName,
                    o.Stage,
                    o.Amount,
                    o.Probability,
                    WeightedPipeline = o.WeightedPipeline,
                    o.ExpectedCloseDate,
                    Owner = o.Owner?.FullName
                })
            });
        }
    }
}
