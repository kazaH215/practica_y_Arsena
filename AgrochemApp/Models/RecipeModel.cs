using System.Collections.Generic;

namespace AgrochemApp.Models
{
    public class RecipeModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public decimal TotalPercent { get; set; }
        public string CreatedAt { get; set; }
        public List<RecipeComponentModel> Components { get; set; }
    }

    public class RecipeComponentModel
    {
        public int Id { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public decimal Percentage { get; set; }
        public decimal Tolerance { get; set; }
        public int OrderNum { get; set; }
    }

    public class CreateRecipeDto
    {
        public int ProductId { get; set; }
        public int Version { get; set; }
        public int CreatedBy { get; set; }
    }

    public class AddComponentDto
    {
        public int RawMaterialId { get; set; }
        public decimal Percentage { get; set; }
        public decimal Tolerance { get; set; }
        public int OrderNum { get; set; }
    }
}