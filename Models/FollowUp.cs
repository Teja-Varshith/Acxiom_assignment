using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace axciom.Models
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        public string RelatedType { get; set; } = "Customer"; // Customer, Lead, Opportunity

        public int? RelatedId { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        [DataType(DataType.Date)]
        public DateTime FollowUpDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        public string Remarks { get; set; } = string.Empty;

        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
