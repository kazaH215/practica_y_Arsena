using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("lab_tests")]
public class LabTest
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("object_type")]
    public string ObjectType { get; set; } = string.Empty;  // 'raw_material' или 'production_batch'

    [Column("object_id")]
    public int ObjectId { get; set; }

    [Column("test_type")]
    public string? TestType { get; set; }

    [Column("status")]
    public string Status { get; set; } = "in_progress";  // in_progress, completed

    [Column("result")]
    public string? Result { get; set; }  // approved, rejected, pending

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("CreatedBy")]
    public virtual User? Author { get; set; }

    public virtual ICollection<LabTestParameter> Parameters { get; set; } = new List<LabTestParameter>();
}