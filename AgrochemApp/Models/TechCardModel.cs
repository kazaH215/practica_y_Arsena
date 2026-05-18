using System.Collections.Generic;

namespace AgrochemApp.Models
{
    public class TechCardModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
        public List<TechStepModel> Steps { get; set; }
    }

    public class TechStepModel
    {
        public int Id { get; set; }
        public string StepType { get; set; }
        public int OrderNum { get; set; }
        public bool IsMandatory { get; set; }
        public string Instructions { get; set; }
        public string PlannedParams { get; set; }
    }

    public class CreateTechCardDto
    {
        public int ProductId { get; set; }
        public int Version { get; set; }
        public int CreatedBy { get; set; }
    }

    public class CreateTechStepDto
    {
        public string StepType { get; set; }
        public int OrderNum { get; set; }
        public bool IsMandatory { get; set; }
        public string Instructions { get; set; }
        public string PlannedParams { get; set; }
    }
}