using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("raw_material_batches")]
public class RawMaterialBatch
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("material_id")]
    public int MaterialId { get; set; }

    [Column("batch_number")]
    public string? BatchNumber { get; set; }

    [Column("supplier")]
    public string? Supplier { get; set; }

    [Column("arrival_date")]
    public DateTime? ArrivalDate { get; set; }

    [Column("quantity")]
    public decimal? Quantity { get; set; }

    [Column("lab_status")]
    public string LabStatus { get; set; } = "pending";

    // Навигационные свойства
    [ForeignKey("MaterialId")]
    public virtual RawMaterial? Material { get; set; }
}
