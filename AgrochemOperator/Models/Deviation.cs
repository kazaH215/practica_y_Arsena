using System;

namespace AgrochemOperator.Models
{
    public class Deviation
    {
        public int BatchId { get; set; }
        public int? StepId { get; set; }
        public int? ExecutionId { get; set; }
        public string Parameter { get; set; }
        public string PlannedValue { get; set; }
        public string ActualValue { get; set; }
        public string Severity { get; set; } // info, warning, critical
        public string Comment { get; set; }
        public int UserId { get; set; }
    }
}