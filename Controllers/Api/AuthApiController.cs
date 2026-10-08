using axciom.DTOs;
using axciom.Models;
using axciom.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace axciom.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public AuthApiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email) ?? await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                await _auditLogger.LogAsync("", model.Email, "ApiLoginFailed", "Auth", result: "Failure", details: "API User not found");
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (!user.IsActive)
            {
                return StatusCode(403, new { message = "Account has been deactivated." });
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _auditLogger.LogAsync(user.Id, user.Email!, "ApiLoginSuccess", "Auth", result: "Success");

                return Ok(new
                {
                    message = "Login successful",
                    userId = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    roles
                });
            }

            if (result.IsLockedOut)
            {
                await _auditLogger.LogAsync(user.Id, user.Email!, "ApiAccountLockout", "Auth", result: "LockedOut");
                return StatusCode(403, new { message = "Account locked out due to multiple failed login attempts." });
            }

            await _auditLogger.LogAsync(user.Id, user.Email!, "ApiLoginFailed", "Auth", result: "Failure");
            return Unauthorized(new { message = "Invalid email or password." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully." });
        }
    }
}
