using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("recipe_components")]
public class RecipeComponent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("recipe_id")]
    public int RecipeId { get; set; }

    [Column("raw_material_id")]
    public int RawMaterialId { get; set; }

    [Column("percentage")]
    public decimal Percentage { get; set; }

    [Column("tolerance")]
    public decimal Tolerance { get; set; } = 0;

    [Column("order_num")]
    public int OrderNum { get; set; }

    // Навигационные свойства
    [ForeignKey("RecipeId")]
    public virtual Recipe? Recipe { get; set; }

    [ForeignKey("RawMaterialId")]
    public virtual RawMaterial? RawMaterial { get; set; }
}