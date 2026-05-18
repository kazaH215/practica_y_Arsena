namespace AgrochemAPI.DTOs;

// DTO для создания заказа
public class CreateProductionOrderDto
{
    public int ProductId { get; set; }
    public int? RecipeId { get; set; }
    public int? TechCardId { get; set; }
    public decimal PlannedQty { get; set; }
}

// DTO для обновления заказа
public class UpdateProductionOrderDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public int? RecipeId { get; set; }
    public int? TechCardId { get; set; }
    public decimal? PlannedQty { get; set; }
    public string? Status { get; set; }
}

// DTO для ответа (список)
public class ProductionOrderListDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? RecipeId { get; set; }
    public string? RecipeVersion { get; set; }
    public int? TechCardId { get; set; }
    public string? TechCardVersion { get; set; }
    public decimal PlannedQty { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? BatchCount { get; set; }  // количество связанных партий
}

// DTO для детального ответа
public class ProductionOrderDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public int? RecipeId { get; set; }
    public string? RecipeVersion { get; set; }
    public int? TechCardId { get; set; }
    public string? TechCardVersion { get; set; }
    public decimal PlannedQty { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<BatchBriefDto> Batches { get; set; } = new();
}

// DTO для краткой информации о партии
public class BatchBriefDto
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal ActualQuantityKg { get; set; }
}