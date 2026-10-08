using axciom.Models;
using axciom.Services;
using axciom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace axciom.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditLogger _auditLogger;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IAuditLogger auditLogger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditLogger = auditLogger;
        }

        // GET: Users
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email!.Contains(searchTerm));
            }

            var usersList = await query.OrderBy(u => u.FullName).ToListAsync();
            var userItemVMs = new List<UserItemViewModel>();

            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                userItemVMs.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    Role = roles.FirstOrDefault() ?? "SalesExecutive",
                    IsActive = user.IsActive,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = user.LockoutEnd,
                    FailedLoginCount = user.AccessFailedCount,
                    CreatedDate = user.CreatedDate
                });
            }

            ViewBag.SearchTerm = searchTerm;
            return View(userItemVMs);
        }

        // POST: Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "ToggleUserStatus", "User", user.Id, "", user.IsActive ? "Active" : "Inactive", "Success", $"User '{user.Email}' set to {(user.IsActive ? "Active" : "Inactive")}");

                TempData["SuccessMessage"] = $"User '{user.Email}' status updated.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Unlock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "UnlockUser", "User", user.Id, "LockedOut", "Unlocked", "Success", $"Unlocked user account '{user.Email}'");

                TempData["SuccessMessage"] = $"User '{user.Email}' has been unlocked.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(newRole))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, newRole);

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "RoleChange", "User", user.Id, string.Join(",", currentRoles), newRole, "Success", $"Changed role for '{user.Email}' to {newRole}");

                TempData["SuccessMessage"] = $"Role updated for '{user.Email}' to {newRole}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogger.LogAsync(currentUser!.Id, currentUser.Email!, "PasswordReset", "User", user.Id, "", "", "Success", $"Reset password for '{user.Email}'");

                    TempData["SuccessMessage"] = $"Password reset successfully for '{user.Email}'.";
                }
                else
                {
                    TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
