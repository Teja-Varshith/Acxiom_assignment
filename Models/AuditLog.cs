using System;
using System.ComponentModel.DataAnnotations;

namespace axciom.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty; // Login, FailedLogin, Logout, Create, Update, Delete, Role Change, Security

        [Required]
        public string EntityName { get; set; } = string.Empty; // User, Customer, Lead, Opportunity, FollowUp, Auth

        public string RecordId { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;

        public string NewValue { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Result { get; set; } = "Success"; // Success, Failure, Blocked

        public string Details { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;
    }
}
