using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("tech_cards")]
public class TechCard
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

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? Author { get; set; }

    public virtual ICollection<TechStep> TechSteps { get; set; } = new List<TechStep>();
}