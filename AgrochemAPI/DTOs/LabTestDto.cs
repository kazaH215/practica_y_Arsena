namespace AgrochemAPI.DTOs;

// DTO для создания испытания
public class CreateLabTestDto
{
    public string ObjectType { get; set; } = string.Empty;  // raw_material или production_batch
    public int ObjectId { get; set; }
    public string? TestType { get; set; }
    public int CreatedBy { get; set; }
    public string? Comment { get; set; }
}

// DTO для параметра испытания
public class LabTestParameterDto
{
    public int? Id { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public decimal? NormMin { get; set; }
    public decimal? NormMax { get; set; }
    public decimal? ActualValue { get; set; }
    public bool? IsPassed { get; set; }
}

// DTO для ввода результатов
public class EnterLabResultDto
{
    public int TestId { get; set; }
    public int UserId { get; set; }
    public List<LabTestParameterDto> Parameters { get; set; } = new();
    public string? Comment { get; set; }
}

// DTO для принятия решения
public class LabDecisionDto
{
    public int TestId { get; set; }
    public int UserId { get; set; }
    public string Result { get; set; } = string.Empty;  // approved, rejected
    public string? Comment { get; set; }
}

// DTO для ответа (список)
public class LabTestListDto
{
    public int Id { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public int ObjectId { get; set; }
    public string? ObjectName { get; set; }
    public string? ObjectNumber { get; set; }
    public string? TestType { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ParametersCount { get; set; }
    public int PassedCount { get; set; }
}

// DTO для детального ответа
public class LabTestDetailDto
{
    public int Id { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public int ObjectId { get; set; }
    public string? ObjectName { get; set; }
    public string? ObjectNumber { get; set; }
    public string? TestType { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? Comment { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LabTestParameterDto> Parameters { get; set; } = new();
    public bool CanMakeDecision { get; set; }
}

// DTO для очереди партий (сырьё или продукция)
public class PendingObjectDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public bool HasActiveTest { get; set; }
    public int? ActiveTestId { get; set; }
}