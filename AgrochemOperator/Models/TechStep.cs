using System.Collections.Generic;

namespace AgrochemOperator.Models
{
    public class TechStep
    {
        public int Id { get; set; }
        public string StepType { get; set; }
        public int OrderNum { get; set; }
        public bool IsMandatory { get; set; }
        public string Instructions { get; set; }
        public string PlannedParams { get; set; } // JSON с плановыми параметрами
        public string Status { get; set; } = "pending"; // для отображения в UI
    }
}