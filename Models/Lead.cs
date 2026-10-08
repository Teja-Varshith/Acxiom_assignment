using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace axciom.Models
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string Source { get; set; } = "Web"; // Web, Referral, Cold Call, Tradeshow, Social

        [Required]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [Required]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        [Range(0, 100000000, ErrorMessage = "Expected value must be a valid positive amount.")]
        public decimal ExpectedValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }
    }
}
