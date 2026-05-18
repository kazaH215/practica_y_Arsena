using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("event_log")]
public class EventLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("batch_id")]
    public int? BatchId { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("BatchId")]
    public virtual ProductionBatch? Batch { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}