using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("production_batches")]
public class ProductionBatch
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("batch_number")]
    public string BatchNumber { get; set; } = string.Empty;

    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("status")]
    public string Status { get; set; } = "planned";

    [Column("actual_quantity_kg")]
    public decimal ActualQuantityKg { get; set; }

    // =====================================================
    // НАВИГАЦИОННЫЕ СВОЙСТВА (ДОБАВИТЬ ЭТИ СТРОКИ)
    // =====================================================
    [ForeignKey("OrderId")]
    public virtual ProductionOrder? Order { get; set; }
}