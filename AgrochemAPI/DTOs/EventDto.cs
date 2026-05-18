namespace AgrochemAPI.DTOs;

// DTO для создания события
public class CreateEventDto
{
    public int? BatchId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserId { get; set; }
}

// DTO для ответа
public class EventListDto
{
    public int Id { get; set; }
    public int? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}