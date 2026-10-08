using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace axciom.Models
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(100)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 1000000000.00, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 50;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        public string Status { get; set; } = "Active"; // Active, Closed

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }

        public string Notes { get; set; } = string.Empty;

        [NotMapped]
        public decimal WeightedPipeline => Amount * Probability / 100m;
    }
}
