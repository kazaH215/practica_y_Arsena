using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("tech_steps")]
public class TechStep
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("tech_card_id")]
    public int TechCardId { get; set; }

    [Column("step_type")]
    public string StepType { get; set; } = string.Empty;

    [Column("order_num")]
    public int OrderNum { get; set; }

    [Column("is_mandatory")]
    public bool IsMandatory { get; set; } = true;

    [Column("instructions")]
    public string? Instructions { get; set; }

    [Column("planned_params")]
    public string? PlannedParams { get; set; }  // JSON строка с параметрами

    // Навигационные свойства
    [ForeignKey("TechCardId")]
    public virtual TechCard? TechCard { get; set; }
}