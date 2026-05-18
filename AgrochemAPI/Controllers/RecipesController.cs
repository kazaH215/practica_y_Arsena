using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using AgrochemAPI.Data;
using AgrochemAPI.Models;
using AgrochemAPI.DTOs;

namespace AgrochemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RecipesController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/recipes - получить все рецептуры
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var recipes = await _context.Recipes
            .Include(r => r.Product)
            .Include(r => r.Author)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecipeDetailDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product != null ? r.Product.Name : null,
                Version = r.Version,
                Status = r.Status,
                TotalPercent = r.TotalPercent,
                CreatedBy = r.CreatedBy,
                AuthorName = r.Author != null ? r.Author.FullName : null,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, count = recipes.Count, data = recipes });
    }

    // =====================================================
    // GET: api/recipes/{id} - получить рецептуру по ID с компонентами
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Product)
            .Include(r => r.Author)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
            return NotFound(new { success = false, message = $"Рецептура с ID {id} не найдена" });

        // Получаем компоненты
        var components = await _context.RecipeComponents
            .Include(c => c.RawMaterial)
            .Where(c => c.RecipeId == id)
            .OrderBy(c => c.OrderNum)
            .Select(c => new RecipeComponentDto
            {
                Id = c.Id,
                RawMaterialId = c.RawMaterialId,
                RawMaterialName = c.RawMaterial != null ? c.RawMaterial.Name : null,
                Percentage = c.Percentage,
                Tolerance = c.Tolerance,
                OrderNum = c.OrderNum
            })
            .ToListAsync();

        var result = new RecipeDetailDto
        {
            Id = recipe.Id,
            ProductId = recipe.ProductId,
            ProductName = recipe.Product?.Name,
            Version = recipe.Version,
            Status = recipe.Status,
            TotalPercent = recipe.TotalPercent,
            CreatedBy = recipe.CreatedBy,
            AuthorName = recipe.Author?.FullName,
            CreatedAt = recipe.CreatedAt,
            Components = components
        };

        return Ok(new { success = true, data = result });
    }

    // =====================================================
    // GET: api/recipes/product/{productId} - рецептуры по продукту
    // =====================================================
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var recipes = await _context.Recipes
            .Where(r => r.ProductId == productId)
            .Include(r => r.Product)
            .OrderByDescending(r => r.Version)
            .Select(r => new RecipeDetailDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product != null ? r.Product.Name : null,
                Version = r.Version,
                Status = r.Status,
                TotalPercent = r.TotalPercent,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, count = recipes.Count, data = recipes });
    }

    // =====================================================
    // GET: api/recipes/active - получить активные (approved)
    // =====================================================
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var recipes = await _context.Recipes
            .Where(r => r.Status == "approved")
            .Include(r => r.Product)
            .Select(r => new RecipeDetailDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product != null ? r.Product.Name : null,
                Version = r.Version,
                Status = r.Status,
                TotalPercent = r.TotalPercent
            })
            .ToListAsync();

        return Ok(new { success = true, count = recipes.Count, data = recipes });
    }

    // =====================================================
    // POST: api/recipes - создать новую рецептуру
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Неверные данные", errors = ModelState });

        // Проверяем, существует ли продукт
        var product = await _context.Products.FindAsync(createDto.ProductId);
        if (product == null)
            return BadRequest(new { success = false, message = $"Продукт с ID {createDto.ProductId} не найден" });

        // Проверяем, существует ли версия
        var existingVersion = await _context.Recipes
            .FirstOrDefaultAsync(r => r.ProductId == createDto.ProductId && r.Version == createDto.Version);

        if (existingVersion != null)
            return BadRequest(new { success = false, message = $"Рецептура для продукта {product.Name} с версией {createDto.Version} уже существует" });

        var recipe = new Recipe
        {
            ProductId = createDto.ProductId,
            Version = createDto.Version,
            Status = "draft",
            TotalPercent = 0,
            CreatedBy = createDto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = recipe.Id },
            new { success = true, message = "Рецептура создана", data = recipe });
    }

    // =====================================================
    // PUT: api/recipes/{id} - обновить рецептуру
    // =====================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecipeDto updateDto)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe == null)
            return NotFound(new { success = false, message = $"Рецептура с ID {id} не найдена" });

        // Нельзя редактировать утверждённую рецептуру
        if (recipe.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя редактировать утверждённую рецептуру" });

        if (updateDto.ProductId.HasValue)
            recipe.ProductId = updateDto.ProductId.Value;
        if (updateDto.Version.HasValue)
            recipe.Version = updateDto.Version.Value;
        if (updateDto.Status != null)
            recipe.Status = updateDto.Status;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Рецептура обновлена", data = recipe });
    }

    // =====================================================
    // POST: api/recipes/{id}/components - добавить компонент
    // =====================================================
    [HttpPost("{id}/components")]
    public async Task<IActionResult> AddComponent(int id, [FromBody] RecipeComponentDto componentDto)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe == null)
            return NotFound(new { success = false, message = $"Рецептура с ID {id} не найдена" });

        if (recipe.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя изменять утверждённую рецептуру" });

        var material = await _context.RawMaterials.FindAsync(componentDto.RawMaterialId);
        if (material == null)
            return BadRequest(new { success = false, message = $"Сырьё с ID {componentDto.RawMaterialId} не найдено" });

        var component = new RecipeComponent
        {
            RecipeId = id,
            RawMaterialId = componentDto.RawMaterialId,
            Percentage = componentDto.Percentage,
            Tolerance = componentDto.Tolerance,
            OrderNum = componentDto.OrderNum
        };

        _context.RecipeComponents.Add(component);
        await _context.SaveChangesAsync();

        // Обновляем total_percent в рецептуре
        await UpdateRecipeTotalPercent(id);

        return Ok(new { success = true, message = "Компонент добавлен", data = component });
    }

    // =====================================================
    // PUT: api/recipes/components/{componentId} - обновить компонент
    // =====================================================
    [HttpPut("components/{componentId}")]
    public async Task<IActionResult> UpdateComponent(int componentId, [FromBody] RecipeComponentDto componentDto)
    {
        var component = await _context.RecipeComponents.FindAsync(componentId);
        if (component == null)
            return NotFound(new { success = false, message = $"Компонент с ID {componentId} не найден" });

        var recipe = await _context.Recipes.FindAsync(component.RecipeId);
        if (recipe?.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя изменять утверждённую рецептуру" });

        component.Percentage = componentDto.Percentage;
        component.Tolerance = componentDto.Tolerance;
        component.OrderNum = componentDto.OrderNum;

        if (componentDto.RawMaterialId > 0)
            component.RawMaterialId = componentDto.RawMaterialId;

        await _context.SaveChangesAsync();

        // Обновляем total_percent
        await UpdateRecipeTotalPercent(component.RecipeId);

        return Ok(new { success = true, message = "Компонент обновлён", data = component });
    }

    // =====================================================
    // DELETE: api/recipes/components/{componentId} - удалить компонент
    // =====================================================
    [HttpDelete("components/{componentId}")]
    public async Task<IActionResult> DeleteComponent(int componentId)
    {
        var component = await _context.RecipeComponents.FindAsync(componentId);
        if (component == null)
            return NotFound(new { success = false, message = $"Компонент с ID {componentId} не найден" });

        var recipe = await _context.Recipes.FindAsync(component.RecipeId);
        if (recipe?.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя изменять утверждённую рецептуру" });

        _context.RecipeComponents.Remove(component);
        await _context.SaveChangesAsync();

        // Обновляем total_percent
        await UpdateRecipeTotalPercent(component.RecipeId);

        return Ok(new { success = true, message = "Компонент удалён" });
    }

    // =====================================================
    // POST: api/recipes/{id}/approve - утвердить рецептуру (вызовет триггер)
    // =====================================================
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] int userId)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe == null)
            return NotFound(new { success = false, message = $"Рецептура с ID {id} не найдена" });

        if (recipe.Status == "approved")
            return BadRequest(new { success = false, message = "Рецептура уже утверждена" });

        try
        {
            // Вызов UPDATE, который активирует триггер (проверка суммы = 100%)
            recipe.Status = "approved";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Рецептура утверждена" });
        }
        catch (DbUpdateException ex)
        {
            // Триггер вернул ошибку
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return BadRequest(new { success = false, message = innerMessage });
        }
    }

    // =====================================================
    // DELETE: api/recipes/{id} - удалить рецептуру (только черновик)
    // =====================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeComponents)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
            return NotFound(new { success = false, message = $"Рецептура с ID {id} не найдена" });

        if (recipe.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя удалить утверждённую рецептуру" });

        // Удаляем связанные компоненты
        _context.RecipeComponents.RemoveRange(recipe.RecipeComponents);
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Рецептура удалена" });
    }

    // =====================================================
    // Приватный метод: обновление total_percent
    // =====================================================
    private async Task UpdateRecipeTotalPercent(int recipeId)
    {
        var total = await _context.RecipeComponents
            .Where(c => c.RecipeId == recipeId)
            .SumAsync(c => c.Percentage);

        var recipe = await _context.Recipes.FindAsync(recipeId);
        if (recipe != null)
        {
            recipe.TotalPercent = total;
            await _context.SaveChangesAsync();
        }
    }
}