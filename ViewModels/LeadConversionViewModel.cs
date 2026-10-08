using System;
using System.ComponentModel.DataAnnotations;

namespace axciom.ViewModels
{
    public class LeadConversionViewModel
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public bool CreateCustomer { get; set; } = true;

        [Required(ErrorMessage = "Customer Name is required.")]
        public string CustomerName { get; set; } = string.Empty;

        public bool CreateOpportunity { get; set; } = true;

        [Required(ErrorMessage = "Opportunity Name is required.")]
        public string OpportunityName { get; set; } = string.Empty;

        [Range(0.01, 1000000000.00, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal OpportunityAmount { get; set; }

        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 50;

        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
    }
}
