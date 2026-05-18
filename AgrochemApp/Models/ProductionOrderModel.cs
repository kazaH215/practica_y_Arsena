using System.Collections.Generic;

namespace AgrochemApp.Models
{
    public class ProductionOrderModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int? RecipeId { get; set; }
        public string RecipeVersion { get; set; }
        public int? TechCardId { get; set; }
        public string TechCardVersion { get; set; }
        public decimal PlannedQty { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
        public int BatchCount { get; set; }
    }

    public class CreateOrderDto
    {
        public int ProductId { get; set; }
        public int? RecipeId { get; set; }
        public int? TechCardId { get; set; }
        public decimal PlannedQty { get; set; }
    }
}