using System.Collections.Generic;

namespace axciom.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public string DateRangeFilter { get; set; } = "ThisMonth"; // Today, ThisWeek, ThisMonth, Custom

        // Chart Data Labels & Values
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusCounts { get; set; } = new();

        public List<string> PipelineStageLabels { get; set; } = new();
        public List<decimal> PipelineStageAmounts { get; set; } = new();

        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesValues { get; set; } = new();
    }
}
