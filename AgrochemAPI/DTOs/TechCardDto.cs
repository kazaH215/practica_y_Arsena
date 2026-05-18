namespace AgrochemAPI.DTOs;

// DTO для создания технологической карты
public class CreateTechCardDto
{
    public int ProductId { get; set; }
    public int Version { get; set; }
    public int CreatedBy { get; set; }
}

// DTO для обновления
public class UpdateTechCardDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public int? Version { get; set; }
    public string? Status { get; set; }
}

// DTO для шага техкарты
public class TechStepDto
{
    public int? Id { get; set; }
    public string StepType { get; set; } = string.Empty;
    public int OrderNum { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string? Instructions { get; set; }
    public string? PlannedParams { get; set; }  // JSON
}

// DTO для полной карточки техкарты (с шагами)
public class TechCardDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TechStepDto> Steps { get; set; } = new();
}

// DTO для смены статуса
public class ChangeTechCardStatusDto
{
    public int TechCardId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public int UserId { get; set; }
}