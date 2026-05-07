using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("audit_log")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("entity_type")]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    public int? EntityId { get; set; }

    [Column("old_state")]
    public string? OldState { get; set; }

    [Column("new_state")]
    public string? NewState { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;
}