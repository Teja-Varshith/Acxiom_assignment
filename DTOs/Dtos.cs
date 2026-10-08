using System;
using System.ComponentModel.DataAnnotations;

namespace axciom.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;
        public string Source { get; set; } = "Web";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 1000000000.00, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 50;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        public DateTime ExpectedCloseDate { get; set; }

        public string Status { get; set; } = "Active";
        public decimal WeightedPipeline => Amount * Probability / 100m;
        public DateTime CreatedDate { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }

    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public string RelatedType { get; set; } = "Customer";

        [Required(ErrorMessage = "Follow-up date is required.")]
        public DateTime FollowUpDate { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; } = string.Empty;

        public string FollowUpType { get; set; } = "Call";
        public string Status { get; set; } = "Planned";
        public string Remarks { get; set; } = string.Empty;
        public string AssignedUserName { get; set; } = string.Empty;
    }
}
