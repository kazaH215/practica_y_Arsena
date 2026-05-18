using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("lab_test_parameters")]
public class LabTestParameter
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("test_id")]
    public int TestId { get; set; }

    [Column("parameter_name")]
    public string ParameterName { get; set; } = string.Empty;

    [Column("norm_min")]
    public decimal? NormMin { get; set; }

    [Column("norm_max")]
    public decimal? NormMax { get; set; }

    [Column("actual_value")]
    public decimal? ActualValue { get; set; }

    [Column("is_passed")]
    public bool? IsPassed { get; set; }

    // Навигационные свойства
    [ForeignKey("TestId")]
    public virtual LabTest? LabTest { get; set; }
}