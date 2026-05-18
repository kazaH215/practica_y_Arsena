using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("production_orders")]
public class ProductionOrder
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("recipe_id")]
    public int? RecipeId { get; set; }

    [Column("tech_card_id")]
    public int? TechCardId { get; set; }

    [Column("planned_qty")]
    public decimal PlannedQty { get; set; }

    [Column("status")]
    public string Status { get; set; } = "planned";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("RecipeId")]
    public virtual Recipe? Recipe { get; set; }

    [ForeignKey("TechCardId")]
    public virtual TechCard? TechCard { get; set; }
}