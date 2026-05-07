using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgrochemAPI.Models;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("type")]
    public string Type { get; set; } = "info";  // info, warning, critical

    [Column("entity_type")]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    public int? EntityId { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}