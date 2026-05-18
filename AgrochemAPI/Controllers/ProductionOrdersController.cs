using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgrochemAPI.Data;
using AgrochemAPI.Models;
using AgrochemAPI.DTOs;

namespace AgrochemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductionOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductionOrdersController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/productionorders - получить все заказы
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.ProductionOrders
            .Include(o => o.Product)
            .Include(o => o.Recipe)
            .Include(o => o.TechCard)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new ProductionOrderListDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : null,
                RecipeId = o.RecipeId,
                RecipeVersion = o.Recipe != null ? o.Recipe.Version.ToString() : null,
                TechCardId = o.TechCardId,
                TechCardVersion = o.TechCard != null ? o.TechCard.Version.ToString() : null,
                PlannedQty = o.PlannedQty,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                BatchCount = _context.ProductionBatches.Count(b => b.OrderId == o.Id)
            })
            .ToListAsync();

        return Ok(new { success = true, count = orders.Count, data = orders });
    }

    // =====================================================
    // GET: api/productionorders/{id} - получить заказ по ID с партиями
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _context.ProductionOrders
            .Include(o => o.Product)
            .Include(o => o.Recipe)
            .Include(o => o.TechCard)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { success = false, message = $"Заказ с ID {id} не найден" });

        // Получаем связанные партии
        var batches = await _context.ProductionBatches
            .Where(b => b.OrderId == id)
            .OrderByDescending(b => b.StartTime)
            .Select(b => new BatchBriefDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNumber,
                Status = b.Status,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                ActualQuantityKg = b.ActualQuantityKg
            })
            .ToListAsync();

        var result = new ProductionOrderDetailDto
        {
            Id = order.Id,
            ProductId = order.ProductId,
            ProductName = order.Product?.Name,
            ProductCode = order.Product?.Code,
            RecipeId = order.RecipeId,
            RecipeVersion = order.Recipe != null ? order.Recipe.Version.ToString() : null,
            TechCardId = order.TechCardId,
            TechCardVersion = order.TechCard != null ? order.TechCard.Version.ToString() : null,
            PlannedQty = order.PlannedQty,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            Batches = batches
        };

        return Ok(new { success = true, data = result });
    }

    // =====================================================
    // GET: api/productionorders/status/{status} - заказы по статусу
    // =====================================================
    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var validStatuses = new[] { "planned", "running", "completed", "cancelled" };
        if (!validStatuses.Contains(status))
            return BadRequest(new { success = false, message = $"Неверный статус. Допустимые: {string.Join(", ", validStatuses)}" });

        var orders = await _context.ProductionOrders
            .Where(o => o.Status == status)
            .Include(o => o.Product)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new ProductionOrderListDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : null,
                RecipeId = o.RecipeId,
                TechCardId = o.TechCardId,
                PlannedQty = o.PlannedQty,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                BatchCount = _context.ProductionBatches.Count(b => b.OrderId == o.Id)
            })
            .ToListAsync();

        return Ok(new { success = true, count = orders.Count, data = orders });
    }

    // =====================================================
    // GET: api/productionorders/product/{productId} - заказы по продукту
    // =====================================================
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return NotFound(new { success = false, message = $"Продукт с ID {productId} не найден" });

        var orders = await _context.ProductionOrders
            .Where(o => o.ProductId == productId)
            .Include(o => o.Product)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new ProductionOrderListDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : null,
                PlannedQty = o.PlannedQty,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                BatchCount = _context.ProductionBatches.Count(b => b.OrderId == o.Id)
            })
            .ToListAsync();

        return Ok(new { success = true, product = product.Name, count = orders.Count, data = orders });
    }

    // =====================================================
    // GET: api/productionorders/active - активные заказы (planned/running)
    // =====================================================
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var orders = await _context.ProductionOrders
            .Where(o => o.Status == "planned" || o.Status == "running")
            .Include(o => o.Product)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new ProductionOrderListDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : null,
                PlannedQty = o.PlannedQty,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                BatchCount = _context.ProductionBatches.Count(b => b.OrderId == o.Id)
            })
            .ToListAsync();

        return Ok(new { success = true, count = orders.Count, data = orders });
    }

    // =====================================================
    // POST: api/productionorders - создать новый заказ
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductionOrderDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Неверные данные", errors = ModelState });

        // Проверяем, существует ли продукт
        var product = await _context.Products.FindAsync(createDto.ProductId);
        if (product == null)
            return BadRequest(new { success = false, message = $"Продукт с ID {createDto.ProductId} не найден" });

        // Проверяем рецептуру (если указана)
        if (createDto.RecipeId.HasValue && createDto.RecipeId.Value > 0)
        {
            var recipe = await _context.Recipes.FindAsync(createDto.RecipeId.Value);
            if (recipe == null)
                return BadRequest(new { success = false, message = $"Рецептура с ID {createDto.RecipeId} не найдена" });

            if (recipe.Status != "approved")
                return BadRequest(new { success = false, message = "Можно использовать только утверждённые рецептуры" });
        }

        // Проверяем техкарту (если указана)
        if (createDto.TechCardId.HasValue && createDto.TechCardId.Value > 0)
        {
            var techCard = await _context.TechCards.FindAsync(createDto.TechCardId.Value);
            if (techCard == null)
                return BadRequest(new { success = false, message = $"Техкарта с ID {createDto.TechCardId} не найдена" });

            if (techCard.Status != "approved")
                return BadRequest(new { success = false, message = "Можно использовать только утверждённые техкарты" });
        }

        var order = new ProductionOrder
        {
            ProductId = createDto.ProductId,
            RecipeId = createDto.RecipeId,
            TechCardId = createDto.TechCardId,
            PlannedQty = createDto.PlannedQty,
            Status = "planned",
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductionOrders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            new { success = true, message = "Заказ создан", data = order });
    }

    // =====================================================
    // PUT: api/productionorders/{id} - обновить заказ
    // =====================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductionOrderDto updateDto)
    {
        var order = await _context.ProductionOrders.FindAsync(id);
        if (order == null)
            return NotFound(new { success = false, message = $"Заказ с ID {id} не найден" });

        // Нельзя редактировать завершённый заказ
        if (order.Status == "completed")
            return BadRequest(new { success = false, message = "Нельзя редактировать завершённый заказ" });

        // Проверяем, нет ли уже запущенных партий
        var hasRunningBatches = await _context.ProductionBatches
            .AnyAsync(b => b.OrderId == id && (b.Status == "running" || b.Status == "planned"));

        if (hasRunningBatches)
            return BadRequest(new { success = false, message = "Нельзя редактировать заказ с существующими партиями" });

        if (updateDto.ProductId.HasValue)
        {
            var product = await _context.Products.FindAsync(updateDto.ProductId.Value);
            if (product == null)
                return BadRequest(new { success = false, message = $"Продукт с ID {updateDto.ProductId} не найден" });
            order.ProductId = updateDto.ProductId.Value;
        }

        if (updateDto.RecipeId.HasValue)
        {
            if (updateDto.RecipeId.Value > 0)
            {
                var recipe = await _context.Recipes.FindAsync(updateDto.RecipeId.Value);
                if (recipe == null)
                    return BadRequest(new { success = false, message = $"Рецептура с ID {updateDto.RecipeId} не найдена" });
                if (recipe.Status != "approved")
                    return BadRequest(new { success = false, message = "Можно использовать только утверждённые рецептуры" });
            }
            order.RecipeId = updateDto.RecipeId.Value == 0 ? null : updateDto.RecipeId.Value;
        }

        if (updateDto.TechCardId.HasValue)
        {
            if (updateDto.TechCardId.Value > 0)
            {
                var techCard = await _context.TechCards.FindAsync(updateDto.TechCardId.Value);
                if (techCard == null)
                    return BadRequest(new { success = false, message = $"Техкарта с ID {updateDto.TechCardId} не найдена" });
                if (techCard.Status != "approved")
                    return BadRequest(new { success = false, message = "Можно использовать только утверждённые техкарты" });
            }
            order.TechCardId = updateDto.TechCardId.Value == 0 ? null : updateDto.TechCardId.Value;
        }

        if (updateDto.PlannedQty.HasValue)
            order.PlannedQty = updateDto.PlannedQty.Value;

        if (updateDto.Status != null)
            order.Status = updateDto.Status;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Заказ обновлён", data = order });
    }

    // =====================================================
    // POST: api/productionorders/{id}/cancel - отменить заказ
    // =====================================================
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _context.ProductionOrders.FindAsync(id);
        if (order == null)
            return NotFound(new { success = false, message = $"Заказ с ID {id} не найден" });

        if (order.Status == "completed")
            return BadRequest(new { success = false, message = "Нельзя отменить завершённый заказ" });

        // Проверяем, нет ли запущенных партий
        var hasRunningBatches = await _context.ProductionBatches
            .AnyAsync(b => b.OrderId == id && b.Status == "running");

        if (hasRunningBatches)
            return BadRequest(new { success = false, message = "Нельзя отменить заказ с запущенными партиями" });

        order.Status = "cancelled";
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Заказ отменён", data = order });
    }

    // =====================================================
    // GET: api/productionorders/dashboard - сводка по заказам
    // =====================================================
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var planned = await _context.ProductionOrders.CountAsync(o => o.Status == "planned");
        var running = await _context.ProductionOrders.CountAsync(o => o.Status == "running");
        var completed = await _context.ProductionOrders.CountAsync(o => o.Status == "completed");
        var cancelled = await _context.ProductionOrders.CountAsync(o => o.Status == "cancelled");

        var totalPlannedQty = await _context.ProductionOrders
            .Where(o => o.Status == "planned" || o.Status == "running")
            .SumAsync(o => o.PlannedQty);

        var recentOrders = await _context.ProductionOrders
            .Include(o => o.Product)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new
            {
                o.Id,
                ProductName = o.Product != null ? o.Product.Name : null,
                o.PlannedQty,
                o.Status,
                o.CreatedAt,
                BatchCount = _context.ProductionBatches.Count(b => b.OrderId == o.Id)
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                counts = new { planned, running, completed, cancelled },
                totalPlannedQuantity = totalPlannedQty,
                recentOrders
            }
        });
    }
}