using System;
using System.Collections.Generic;

namespace axciom.ViewModels
{
    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UserManagementViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
    }
}
