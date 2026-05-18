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


public class TechCardsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TechCardsController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/techcards - получить все техкарты
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var techCards = await _context.TechCards
            .Include(t => t.Product)
            .Include(t => t.Author)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TechCardDetailDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product != null ? t.Product.Name : null,
                Version = t.Version,
                Status = t.Status,
                CreatedBy = t.CreatedBy,
                AuthorName = t.Author != null ? t.Author.FullName : null,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, count = techCards.Count, data = techCards });
    }

    // =====================================================
    // GET: api/techcards/{id} - получить техкарту по ID с шагами
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var techCard = await _context.TechCards
            .Include(t => t.Product)
            .Include(t => t.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (techCard == null)
            return NotFound(new { success = false, message = $"Техкарта с ID {id} не найдена" });

        // Получаем шаги
        var steps = await _context.TechSteps
            .Where(s => s.TechCardId == id)
            .OrderBy(s => s.OrderNum)
            .Select(s => new TechStepDto
            {
                Id = s.Id,
                StepType = s.StepType,
                OrderNum = s.OrderNum,
                IsMandatory = s.IsMandatory,
                Instructions = s.Instructions,
                PlannedParams = s.PlannedParams
            })
            .ToListAsync();

        var result = new TechCardDetailDto
        {
            Id = techCard.Id,
            ProductId = techCard.ProductId,
            ProductName = techCard.Product?.Name,
            Version = techCard.Version,
            Status = techCard.Status,
            CreatedBy = techCard.CreatedBy,
            AuthorName = techCard.Author?.FullName,
            CreatedAt = techCard.CreatedAt,
            Steps = steps
        };

        return Ok(new { success = true, data = result });
    }

    // =====================================================
    // GET: api/techcards/product/{productId} - техкарты по продукту
    // =====================================================
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var techCards = await _context.TechCards
            .Where(t => t.ProductId == productId)
            .Include(t => t.Product)
            .OrderByDescending(t => t.Version)
            .Select(t => new TechCardDetailDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product != null ? t.Product.Name : null,
                Version = t.Version,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, count = techCards.Count, data = techCards });
    }

    // =====================================================
    // GET: api/techcards/active - получить активные (approved)
    // =====================================================
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var techCards = await _context.TechCards
            .Where(t => t.Status == "approved")
            .Include(t => t.Product)
            .Select(t => new TechCardDetailDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product != null ? t.Product.Name : null,
                Version = t.Version,
                Status = t.Status
            })
            .ToListAsync();

        return Ok(new { success = true, count = techCards.Count, data = techCards });
    }

    // =====================================================
    // POST: api/techcards - создать новую техкарту
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTechCardDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Неверные данные", errors = ModelState });

        // Проверяем, существует ли продукт
        var product = await _context.Products.FindAsync(createDto.ProductId);
        if (product == null)
            return BadRequest(new { success = false, message = $"Продукт с ID {createDto.ProductId} не найден" });

        // Проверяем, существует ли версия
        var existingVersion = await _context.TechCards
            .FirstOrDefaultAsync(t => t.ProductId == createDto.ProductId && t.Version == createDto.Version);

        if (existingVersion != null)
            return BadRequest(new { success = false, message = $"Техкарта для продукта {product.Name} с версией {createDto.Version} уже существует" });

        var techCard = new TechCard
        {
            ProductId = createDto.ProductId,
            Version = createDto.Version,
            Status = "draft",
            CreatedBy = createDto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.TechCards.Add(techCard);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = techCard.Id },
            new { success = true, message = "Технологическая карта создана", data = techCard });
    }

    // =====================================================
    // PUT: api/techcards/{id} - обновить техкарту
    // =====================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTechCardDto updateDto)
    {
        var techCard = await _context.TechCards.FindAsync(id);
        if (techCard == null)
            return NotFound(new { success = false, message = $"Техкарта с ID {id} не найдена" });

        // Нельзя редактировать утверждённую техкарту
        if (techCard.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя редактировать утверждённую технологическую карту" });

        if (updateDto.ProductId.HasValue)
            techCard.ProductId = updateDto.ProductId.Value;
        if (updateDto.Version.HasValue)
            techCard.Version = updateDto.Version.Value;
        if (updateDto.Status != null)
            techCard.Status = updateDto.Status;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Техкарта обновлена", data = techCard });
    }

    // =====================================================
    // POST: api/techcards/{id}/steps - добавить шаг
    // =====================================================
    [HttpPost("{id}/steps")]
    public async Task<IActionResult> AddStep(int id, [FromBody] TechStepDto stepDto)
    {
        var techCard = await _context.TechCards.FindAsync(id);
        if (techCard == null)
            return NotFound(new { success = false, message = $"Техкарта с ID {id} не найдена" });

        if (techCard.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя изменять утверждённую техкарту" });

        var step = new TechStep
        {
            TechCardId = id,
            StepType = stepDto.StepType,
            OrderNum = stepDto.OrderNum,
            IsMandatory = stepDto.IsMandatory,
            Instructions = stepDto.Instructions,
            PlannedParams = stepDto.PlannedParams
        };

        _context.TechSteps.Add(step);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Шаг добавлен", data = step });
    }

    // =====================================================
    // PUT: api/techcards/steps/{stepId} - обновить шаг
    // =====================================================
    [HttpPut("steps/{stepId}")]
    public async Task<IActionResult> UpdateStep(int stepId, [FromBody] TechStepDto stepDto)
    {
        var step = await _context.TechSteps.FindAsync(stepId);
        if (step == null)
            return NotFound(new { success = false, message = $"Шаг с ID {stepId} не найден" });

        var techCard = await _context.TechCards.FindAsync(step.TechCardId);
        if (techCard?.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя изменять утверждённую техкарту" });

        step.StepType = stepDto.StepType;
        step.OrderNum = stepDto.OrderNum;
        step.IsMandatory = stepDto.IsMandatory;
        step.Instructions = stepDto.Instructions;
        step.PlannedParams = stepDto.PlannedParams;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Шаг обновлён", data = step });
    }

    // =====================================================
    // DELETE: api/techcards/steps/{stepId} - удалить шаг
    // =====================================================
    [HttpDelete("steps/{stepId}")]
    public async Task<IActionResult> DeleteStep(int stepId)
    {
        var step = await _context.TechSteps.FindAsync(stepId);
        if (step == null)
            return NotFound(new { success = false, message = $"Шаг с ID {stepId} не найден" });

        var techCard = await _context.TechCards.FindAsync(step.TechCardId);
        if (techCard?.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя изменять утверждённую техкарту" });

        _context.TechSteps.Remove(step);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Шаг удалён" });
    }

    // =====================================================
    // POST: api/techcards/{id}/approve - утвердить техкарту
    // =====================================================
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] int userId)
    {
        var techCard = await _context.TechCards.FindAsync(id);
        if (techCard == null)
            return NotFound(new { success = false, message = $"Техкарта с ID {id} не найдена" });

        if (techCard.Status == "approved")
            return BadRequest(new { success = false, message = "Техкарта уже утверждена" });

        // Проверяем, есть ли хотя бы один шаг
        var hasSteps = await _context.TechSteps.AnyAsync(s => s.TechCardId == id);
        if (!hasSteps)
            return BadRequest(new { success = false, message = "Нельзя утвердить техкарту без шагов" });

        techCard.Status = "approved";
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Технологическая карта утверждена" });
    }

    // =====================================================
    // DELETE: api/techcards/{id} - удалить техкарту (только черновик)
    // =====================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var techCard = await _context.TechCards
            .Include(t => t.TechSteps)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (techCard == null)
            return NotFound(new { success = false, message = $"Техкарта с ID {id} не найдена" });

        if (techCard.Status == "approved")
            return BadRequest(new { success = false, message = "Нельзя удалить утверждённую техкарту" });

        // Удаляем связанные шаги
        _context.TechSteps.RemoveRange(techCard.TechSteps);
        _context.TechCards.Remove(techCard);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Техкарта удалена" });
    }
}