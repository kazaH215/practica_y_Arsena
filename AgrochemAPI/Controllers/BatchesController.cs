using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using AgrochemAPI.Data;
using AgrochemAPI.Models;

namespace AgrochemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BatchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BatchesController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/batches - получить все партии
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var batches = await _context.ProductionBatches
            .OrderByDescending(b => b.StartTime)
            .ToListAsync();

        return Ok(new { success = true, data = batches });
    }

    // =====================================================
    // GET: api/batches/active - получить активные партии (running/planned)
    // =====================================================
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var batches = await _context.ProductionBatches
            .Where(b => b.Status == "running" || b.Status == "planned")
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        return Ok(new { success = true, data = batches });
    }

    // =====================================================
    // GET: api/batches/{id} - получить партию по ID
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var batch = await _context.ProductionBatches.FindAsync(id);
        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с ID {id} не найдена" });

        return Ok(new { success = true, data = batch });
    }

    // =====================================================
    // GET: api/batches/number/{batchNumber} - получить партию по номеру
    // =====================================================
    [HttpGet("number/{batchNumber}")]
    public async Task<IActionResult> GetByNumber(string batchNumber)
    {
        var batch = await _context.ProductionBatches
            .FirstOrDefaultAsync(b => b.BatchNumber == batchNumber);

        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с номером {batchNumber} не найдена" });

        return Ok(new { success = true, data = batch });
    }

    // =====================================================
    // POST: api/batches - создать новую партию
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBatchDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Неверные данные", errors = ModelState });

        var batch = new ProductionBatch
        {
            BatchNumber = createDto.BatchNumber,
            OrderId = createDto.OrderId,
            Status = "planned",
            ActualQuantityKg = 0,
            StartTime = null,
            EndTime = null
        };

        // Проверяем, нет ли уже такой партии
        var existing = await _context.ProductionBatches
            .FirstOrDefaultAsync(b => b.BatchNumber == createDto.BatchNumber);

        if (existing != null)
            return BadRequest(new { success = false, message = $"Партия с номером {createDto.BatchNumber} уже существует" });

        _context.ProductionBatches.Add(batch);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = batch.Id },
            new { success = true, data = batch });
    }

    // =====================================================
    // POST: api/batches/start - запустить партию (вызов процедуры)
    // =====================================================
    [HttpPost("start")]
    public async Task<IActionResult> StartBatch([FromBody] StartBatchDto startDto)
    {
        try
        {
            // Проверяем существование партии
            var batch = await _context.ProductionBatches.FindAsync(startDto.BatchId);
            if (batch == null)
                return NotFound(new { success = false, message = $"Партия с ID {startDto.BatchId} не найдена" });

            // Проверяем статус
            if (batch.Status != "planned")
                return BadRequest(new { success = false, message = $"Партия {batch.BatchNumber} уже запущена или завершена. Текущий статус: {batch.Status}" });

            // Вызов хранимой процедуры sp_start_production_batch
            var sql = "EXEC sp_start_production_batch @p_batch_id, @p_user_id";
            var parameters = new[]
            {
                new SqlParameter("@p_batch_id", startDto.BatchId),
                new SqlParameter("@p_user_id", startDto.UserId)
            };

            await _context.Database.ExecuteSqlRawAsync(sql, parameters);

            // Обновляем локальный объект (чтобы вернуть актуальные данные)
            batch.Status = "running";
            batch.StartTime = DateTime.Now;

            return Ok(new
            {
                success = true,
                message = $"Партия {batch.BatchNumber} успешно запущена",
                data = batch
            });
        }
        catch (SqlException ex)
        {
            // Ошибка из SQL (RAISERROR в процедуре)
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера", error = ex.Message });
        }
    }

    // =====================================================
    // POST: api/batches/complete - завершить партию (вызов процедуры с проверкой лаборатории)
    // =====================================================
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteBatch([FromBody] CompleteBatchDto completeDto)
    {
        try
        {
            // Проверяем существование партии
            var batch = await _context.ProductionBatches.FindAsync(completeDto.BatchId);
            if (batch == null)
                return NotFound(new { success = false, message = $"Партия с ID {completeDto.BatchId} не найдена" });

            // Проверяем статус
            if (batch.Status == "completed")
                return BadRequest(new { success = false, message = $"Партия {batch.BatchNumber} уже завершена" });

            if (batch.Status == "planned")
                return BadRequest(new { success = false, message = $"Партия {batch.BatchNumber} ещё не запущена" });

            // Вызов хранимой процедуры sp_complete_batch_with_lab_check
            var sql = "EXEC sp_complete_batch_with_lab_check @p_batch_id, @p_user_id";
            var parameters = new[]
            {
                new SqlParameter("@p_batch_id", completeDto.BatchId),
                new SqlParameter("@p_user_id", completeDto.UserId)
            };

            await _context.Database.ExecuteSqlRawAsync(sql, parameters);

            // Обновляем локальный объект
            batch.Status = "completed";
            batch.EndTime = DateTime.Now;

            return Ok(new
            {
                success = true,
                message = $"Партия {batch.BatchNumber} успешно завершена",
                data = batch
            });
        }
        catch (SqlException ex)
        {
            // Ошибка из SQL (например, нет лабораторного решения)
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера", error = ex.Message });
        }
    }

    // =====================================================
    // GET: api/batches/status/running - получить только запущенные партии
    // =====================================================
    [HttpGet("status/running")]
    public async Task<IActionResult> GetRunningBatches()
    {
        var batches = await _context.ProductionBatches
            .Where(b => b.Status == "running")
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        return Ok(new { success = true, count = batches.Count, data = batches });
    }

    // =====================================================
    // GET: api/batches/status/completed - получить завершённые партии
    // =====================================================
    [HttpGet("status/completed")]
    public async Task<IActionResult> GetCompletedBatches()
    {
        var batches = await _context.ProductionBatches
            .Where(b => b.Status == "completed")
            .OrderByDescending(b => b.EndTime)
            .ToListAsync();

        return Ok(new { success = true, count = batches.Count, data = batches });
    }

    // =====================================================
    // GET: api/batches/dashboard - статистика для главной страницы технолога
    // =====================================================
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var runningCount = await _context.ProductionBatches.CountAsync(b => b.Status == "running");
        var plannedCount = await _context.ProductionBatches.CountAsync(b => b.Status == "planned");
        var completedCount = await _context.ProductionBatches.CountAsync(b => b.Status == "completed");

        // Партии с отклонениями (если есть таблица deviations)
        var batchesWithDeviations = 0;
        // TODO: когда добавишь таблицу deviations, раскомментируй:
        // batchesWithDeviations = await _context.Deviations.Select(d => d.BatchId).Distinct().CountAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                running = runningCount,
                planned = plannedCount,
                completed = completedCount,
                withDeviations = batchesWithDeviations,
                total = runningCount + plannedCount + completedCount
            }
        });
    }
}

// =====================================================
// DTO (Data Transfer Objects) для запросов
// =====================================================

public class CreateBatchDto
{
    public string BatchNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
}

public class StartBatchDto
{
    public int BatchId { get; set; }
    public int UserId { get; set; }
}

public class CompleteBatchDto
{
    public int BatchId { get; set; }
    public int UserId { get; set; }
}