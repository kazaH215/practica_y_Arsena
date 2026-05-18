using System;

namespace AgrochemOperator.Models
{
    public class BatchStepExecution
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public int StepId { get; set; }
        public string ActualParams { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } // pending, in_progress, completed, skipped
    }
}