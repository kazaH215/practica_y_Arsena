namespace AgrochemAPI.DTOs;

// DTO для создания отклонения
public class CreateDeviationDto
{
    public int BatchId { get; set; }
    public int? StepId { get; set; }
    public int? ExecutionId { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string? PlannedValue { get; set; }
    public string ActualValue { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";  // info, warning, critical
    public string? Comment { get; set; }
    public int UserId { get; set; }
}

// DTO для ответа (список)
public class DeviationListDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public int? StepId { get; set; }
    public string? StepType { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string? PlannedValue { get; set; }
    public string? ActualValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

// DTO для детального ответа
public class DeviationDetailDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public string? ProductName { get; set; }
    public int? StepId { get; set; }
    public string? StepType { get; set; }
    public int? StepOrderNum { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string? PlannedValue { get; set; }
    public string? ActualValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DTO для статистики отклонений
public class DeviationStatsDto
{
    public int Total { get; set; }
    public int Critical { get; set; }
    public int Warning { get; set; }
    public int Info { get; set; }
    public int BatchesWithDeviations { get; set; }
    public List<TopDeviationParameterDto> TopParameters { get; set; } = new();
}

public class TopDeviationParameterDto
{
    public string Parameter { get; set; } = string.Empty;
    public int Count { get; set; }
}