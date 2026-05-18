namespace AgrochemAPI.DTOs;

// DTO для запуска шага
public class StartStepDto
{
    public int BatchId { get; set; }
    public int StepId { get; set; }
    public int UserId { get; set; }
}

// DTO для завершения шага с вводом параметров
public class CompleteStepDto
{
    public int ExecutionId { get; set; }
    public int UserId { get; set; }
    public string? ActualParams { get; set; }  // JSON строка
    public string? Comment { get; set; }
}

// DTO для пропуска шага (если не обязательный)
public class SkipStepDto
{
    public int ExecutionId { get; set; }
    public int UserId { get; set; }
    public string? Reason { get; set; }
}

// DTO для обновления параметров шага (без завершения)
public class UpdateStepParamsDto
{
    public int ExecutionId { get; set; }
    public int UserId { get; set; }
    public string ActualParams { get; set; } = string.Empty;
}

// DTO для ответа (информация о шаге)
public class BatchStepResponseDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public int StepId { get; set; }
    public string? StepType { get; set; }
    public int OrderNum { get; set; }
    public bool IsMandatory { get; set; }
    public string? Instructions { get; set; }
    public string? PlannedParams { get; set; }
    public string? ActualParams { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

// DTO для списка шагов партии
public class BatchStepListDto
{
    public int Id { get; set; }
    public int StepId { get; set; }
    public string? StepType { get; set; }
    public int OrderNum { get; set; }
    public bool IsMandatory { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool HasDeviation { get; set; }
}