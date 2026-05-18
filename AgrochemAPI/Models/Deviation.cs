using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("deviations")]
public class Deviation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("batch_id")]
    public int BatchId { get; set; }

    [Column("step_id")]
    public int? StepId { get; set; }

    [Column("parameter")]
    public string Parameter { get; set; } = string.Empty;

    [Column("planned_value")]
    public string? PlannedValue { get; set; }

    [Column("actual_value")]
    public string? ActualValue { get; set; }

    [Column("severity")]
    public string Severity { get; set; } = "warning";  // info, warning, critical

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("BatchId")]
    public virtual ProductionBatch? Batch { get; set; }

    [ForeignKey("StepId")]
    public virtual TechStep? TechStep { get; set; }
}