using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("batch_step_execution")]
public class BatchStepExecution
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("batch_id")]
    public int BatchId { get; set; }

    [Column("step_id")]
    public int StepId { get; set; }

    [Column("actual_params")]
    public string? ActualParams { get; set; }  // JSON строка с фактическими параметрами

    [Column("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";  // pending, in_progress, completed, skipped

    // Навигационные свойства
    [ForeignKey("BatchId")]
    public virtual ProductionBatch? Batch { get; set; }

    [ForeignKey("StepId")]
    public virtual TechStep? TechStep { get; set; }
}