namespace AgrochemAPI.DTOs;

// DTO для создания/обновления рецептуры
public class CreateRecipeDto
{
    public int ProductId { get; set; }
    public int Version { get; set; }
    public int CreatedBy { get; set; }
}

public class UpdateRecipeDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public int? Version { get; set; }
    public string? Status { get; set; }
}

// DTO для компонентов рецептуры
public class RecipeComponentDto
{
    public int? Id { get; set; }
    public int RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public decimal Percentage { get; set; }
    public decimal Tolerance { get; set; }
    public int OrderNum { get; set; }
}

// DTO для полной карточки рецептуры (с компонентами)
public class RecipeDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPercent { get; set; }
    public int? CreatedBy { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RecipeComponentDto> Components { get; set; } = new();
}

// DTO для смены статуса
public class ChangeRecipeStatusDto
{
    public int RecipeId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public int UserId { get; set; }
}