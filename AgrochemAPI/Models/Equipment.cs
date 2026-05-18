using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("equipment")]
public class Equipment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("line_id")]
    public string? LineId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";
}