using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("recipes")]
public class Recipe
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("total_percent")]
    public decimal TotalPercent { get; set; } = 0;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? Author { get; set; }

    // 👇 ЭТО СВОЙСТВО БЫЛО ОТСУТСТВУЕТ — ДОБАВЛЯЕМ!
    public virtual ICollection<RecipeComponent> RecipeComponents { get; set; } = new List<RecipeComponent>();
}