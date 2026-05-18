namespace AgrochemAPI.DTOs;

// DTO для создания уведомления
public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
}

// DTO для ответа
public class NotificationListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DTO для отметки о прочтении
public class MarkReadDto
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
}