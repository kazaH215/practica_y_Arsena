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
public class LabTestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LabTestsController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/labtests - получить все испытания
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tests = await _context.LabTests
            .Include(t => t.Author)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new LabTestListDto
            {
                Id = t.Id,
                ObjectType = t.ObjectType,
                ObjectId = t.ObjectId,
                TestType = t.TestType,
                Status = t.Status,
                Result = t.Result,
                AuthorName = t.Author != null ? t.Author.FullName : null,
                CreatedAt = t.CreatedAt,
                ParametersCount = t.Parameters.Count,
                PassedCount = t.Parameters.Count(p => p.IsPassed == true)
            })
            .ToListAsync();

        return Ok(new { success = true, count = tests.Count, data = tests });
    }

    // =====================================================
    // GET: api/labtests/{id} - получить испытание по ID
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var test = await _context.LabTests
            .Include(t => t.Author)
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null)
            return NotFound(new { success = false, message = $"Испытание с ID {id} не найдено" });

        // Получаем имя объекта
        string? objectName = null;
        string? objectNumber = null;

        if (test.ObjectType == "production_batch")
        {
            var batch = await _context.ProductionBatches.FindAsync(test.ObjectId);
            objectNumber = batch?.BatchNumber;
            var order = batch != null ? await _context.ProductionOrders.FindAsync(batch.OrderId) : null;
            var product = order != null ? await _context.Products.FindAsync(order.ProductId) : null;
            objectName = product?.Name;
        }
        else if (test.ObjectType == "raw_material")
        {
            var rawMaterialBatch = await _context.RawMaterialBatches.FindAsync(test.ObjectId);
            objectNumber = rawMaterialBatch?.BatchNumber;
            var material = rawMaterialBatch != null ? await _context.RawMaterials.FindAsync(rawMaterialBatch.MaterialId) : null;
            objectName = material?.Name;
        }

        var canMakeDecision = test.Status == "completed" && test.Result == null;

        var result = new LabTestDetailDto
        {
            Id = test.Id,
            ObjectType = test.ObjectType,
            ObjectId = test.ObjectId,
            ObjectName = objectName,
            ObjectNumber = objectNumber,
            TestType = test.TestType,
            Status = test.Status,
            Result = test.Result,
            Comment = test.Comment,
            AuthorName = test.Author?.FullName,
            CreatedAt = test.CreatedAt,
            Parameters = test.Parameters.Select(p => new LabTestParameterDto
            {
                Id = p.Id,
                ParameterName = p.ParameterName,
                NormMin = p.NormMin,
                NormMax = p.NormMax,
                ActualValue = p.ActualValue,
                IsPassed = p.IsPassed
            }).ToList(),
            CanMakeDecision = canMakeDecision
        };

        return Ok(new { success = true, data = result });
    }

    // =====================================================
    // GET: api/labtests/object/{objectType}/{objectId} - испытания по объекту
    // =====================================================
    [HttpGet("object/{objectType}/{objectId}")]
    public async Task<IActionResult> GetByObject(string objectType, int objectId)
    {
        var tests = await _context.LabTests
            .Where(t => t.ObjectType == objectType && t.ObjectId == objectId)
            .Include(t => t.Author)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new LabTestListDto
            {
                Id = t.Id,
                ObjectType = t.ObjectType,
                ObjectId = t.ObjectId,
                TestType = t.TestType,
                Status = t.Status,
                Result = t.Result,
                AuthorName = t.Author != null ? t.Author.FullName : null,
                CreatedAt = t.CreatedAt,
                ParametersCount = t.Parameters.Count,
                PassedCount = t.Parameters.Count(p => p.IsPassed == true)
            })
            .ToListAsync();

        return Ok(new { success = true, count = tests.Count, data = tests });
    }

    // =====================================================
    // GET: api/labtests/pending/raw-materials - партии сырья, ожидающие анализа
    // =====================================================
    [HttpGet("pending/raw-materials")]
    public async Task<IActionResult> GetPendingRawMaterials()
    {
        // Получаем все партии сырья
        var rawMaterialBatches = await _context.RawMaterialBatches
            .Include(r => r.Material)
            .ToListAsync();

        var result = new List<PendingObjectDto>();

        foreach (var batch in rawMaterialBatches)
        {
            // Проверяем, есть ли активное испытание
            var activeTest = await _context.LabTests
                .FirstOrDefaultAsync(t => t.ObjectType == "raw_material"
                    && t.ObjectId == batch.Id
                    && t.Result == null);

            // Если статус pending или есть активное испытание
            if (batch.LabStatus == "pending" || (activeTest != null && activeTest.Status != "completed"))
            {
                result.Add(new PendingObjectDto
                {
                    Id = batch.Id,
                    Number = batch.BatchNumber ?? $"RM-{batch.Id}",
                    Name = batch.Material?.Name ?? "Неизвестно",
                    Type = "raw_material",
                    Date = batch.ArrivalDate,
                    Supplier = batch.Supplier ?? "Не указан",
                    Quantity = batch.Quantity,
                    CurrentStatus = batch.LabStatus,
                    HasActiveTest = activeTest != null,
                    ActiveTestId = activeTest?.Id
                });
            }
        }

        return Ok(new { success = true, count = result.Count, data = result });
    }

    // =====================================================
    // GET: api/labtests/pending/batches - партии продукции, ожидающие анализа
    // =====================================================
    [HttpGet("pending/batches")]
    public async Task<IActionResult> GetPendingBatches()
    {
        // Получаем завершённые партии, у которых нет лабораторного решения
        var batches = await _context.ProductionBatches
            .Where(b => b.Status == "completed")
            .ToListAsync();

        var result = new List<PendingObjectDto>();

        foreach (var batch in batches)
        {
            // Проверяем, есть ли лабораторное решение
            var labTest = await _context.LabTests
                .FirstOrDefaultAsync(t => t.ObjectType == "production_batch"
                    && t.ObjectId == batch.Id
                    && t.Result != null);

            if (labTest == null)
            {
                var order = await _context.ProductionOrders.FindAsync(batch.OrderId);
                var product = order != null ? await _context.Products.FindAsync(order.ProductId) : null;

                result.Add(new PendingObjectDto
                {
                    Id = batch.Id,
                    Number = batch.BatchNumber,
                    Name = product?.Name ?? "Неизвестно",
                    Type = "production_batch",
                    Date = batch.EndTime,
                    Supplier = "",
                    Quantity = batch.ActualQuantityKg,
                    CurrentStatus = batch.Status,
                    HasActiveTest = false,
                    ActiveTestId = null
                });
            }
        }

        return Ok(new { success = true, count = result.Count, data = result });
    }

    // =====================================================
    // POST: api/labtests - создать новое испытание
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLabTestDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Неверные данные", errors = ModelState });

        // Проверяем, существует ли объект
        if (createDto.ObjectType == "production_batch")
        {
            var batch = await _context.ProductionBatches.FindAsync(createDto.ObjectId);
            if (batch == null)
                return BadRequest(new { success = false, message = $"Партия с ID {createDto.ObjectId} не найдена" });
        }
        else if (createDto.ObjectType == "raw_material")
        {
            var material = await _context.RawMaterialBatches.FindAsync(createDto.ObjectId);
            if (material == null)
                return BadRequest(new { success = false, message = $"Партия сырья с ID {createDto.ObjectId} не найдена" });
        }
        else
        {
            return BadRequest(new { success = false, message = "Неверный тип объекта. Допустимые: production_batch, raw_material" });
        }

        // Проверяем, нет ли уже незавершённого испытания
        var existingTest = await _context.LabTests
            .FirstOrDefaultAsync(t => t.ObjectType == createDto.ObjectType
                && t.ObjectId == createDto.ObjectId
                && t.Result == null);

        if (existingTest != null)
            return BadRequest(new { success = false, message = "Для этого объекта уже есть незавершённое испытание", testId = existingTest.Id });

        var test = new LabTest
        {
            ObjectType = createDto.ObjectType,
            ObjectId = createDto.ObjectId,
            TestType = createDto.TestType,
            Status = "in_progress",
            CreatedBy = createDto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            Comment = createDto.Comment
        };

        _context.LabTests.Add(test);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = test.Id },
            new { success = true, message = "Испытание создано", data = test });
    }

    // =====================================================
    // POST: api/labtests/{id}/parameters - добавить параметры к испытанию
    // =====================================================
    [HttpPost("{id}/parameters")]
    public async Task<IActionResult> AddParameters(int id, [FromBody] List<LabTestParameterDto> parameters)
    {
        var test = await _context.LabTests.FindAsync(id);
        if (test == null)
            return NotFound(new { success = false, message = $"Испытание с ID {id} не найдено" });

        if (test.Status == "completed")
            return BadRequest(new { success = false, message = "Нельзя изменять завершённое испытание" });

        foreach (var param in parameters)
        {
            var parameter = new LabTestParameter
            {
                TestId = id,
                ParameterName = param.ParameterName,
                NormMin = param.NormMin,
                NormMax = param.NormMax
            };
            _context.LabTestParameters.Add(parameter);
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Добавлено {parameters.Count} параметров" });
    }

    // =====================================================
    // PUT: api/labtests/results - ввод результатов анализов
    // =====================================================
    [HttpPut("results")]
    public async Task<IActionResult> EnterResults([FromBody] EnterLabResultDto resultDto)
    {
        var test = await _context.LabTests
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == resultDto.TestId);

        if (test == null)
            return NotFound(new { success = false, message = $"Испытание с ID {resultDto.TestId} не найдено" });

        if (test.Status == "completed")
            return BadRequest(new { success = false, message = "Испытание уже завершено" });

        // Обновляем параметры
        foreach (var paramDto in resultDto.Parameters)
        {
            if (paramDto.Id.HasValue)
            {
                var param = test.Parameters.FirstOrDefault(p => p.Id == paramDto.Id.Value);
                if (param != null)
                {
                    param.ActualValue = paramDto.ActualValue;
                    param.IsPassed = CheckIfPassed(param.NormMin, param.NormMax, paramDto.ActualValue);
                }
            }
        }

        if (!string.IsNullOrEmpty(resultDto.Comment))
            test.Comment = resultDto.Comment;

        test.Status = "completed";
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Результаты сохранены. Ожидает решения лаборатории." });
    }

    // =====================================================
    // POST: api/labtests/decision - принять решение по испытанию
    // =====================================================
    [HttpPost("decision")]
    public async Task<IActionResult> MakeDecision([FromBody] LabDecisionDto decisionDto)
    {
        if (decisionDto.Result != "approved" && decisionDto.Result != "rejected")
            return BadRequest(new { success = false, message = "Решение должно быть 'approved' или 'rejected'" });

        var test = await _context.LabTests
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == decisionDto.TestId);

        if (test == null)
            return NotFound(new { success = false, message = $"Испытание с ID {decisionDto.TestId} не найдено" });

        if (test.Status != "completed")
            return BadRequest(new { success = false, message = "Нельзя принять решение: испытание не завершено" });

        if (test.Result != null)
            return BadRequest(new { success = false, message = "Решение уже принято" });

        // При блокировке комментарий обязателен
        if (decisionDto.Result == "rejected" && string.IsNullOrEmpty(decisionDto.Comment))
            return BadRequest(new { success = false, message = "При блокировке партии необходимо указать причину" });

        test.Result = decisionDto.Result;
        if (!string.IsNullOrEmpty(decisionDto.Comment))
            test.Comment = (test.Comment != null ? test.Comment + "\n" : "") + $"Решение: {decisionDto.Comment}";

        await _context.SaveChangesAsync();

        // Обновляем статус объекта (партии или сырья)
        if (test.ObjectType == "production_batch")
        {
            // Для партии ничего не делаем, статус меняет технолог через процедуру
            // Но можно обновить что-то при необходимости
        }
        else if (test.ObjectType == "raw_material")
        {
            var rawMaterialBatch = await _context.RawMaterialBatches.FindAsync(test.ObjectId);
            if (rawMaterialBatch != null)
            {
                rawMaterialBatch.LabStatus = decisionDto.Result == "approved" ? "approved" : "rejected";
                await _context.SaveChangesAsync();
            }
        }

        // Записываем в аудит
        var audit = new AuditLog
        {
            UserId = decisionDto.UserId,
            Action = "lab_decision",
            EntityType = "lab_test",
            EntityId = test.Id,
            NewState = $"{{\"result\":\"{decisionDto.Result}\",\"comment\":\"{decisionDto.Comment}\"}}",
            Timestamp = DateTime.Now
        };
        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Решение принято: {(decisionDto.Result == "approved" ? "✅ Партия одобрена" : "❌ Партия заблокирована")}" });
    }

    // =====================================================
    // GET: api/labtests/statistics - статистика лаборатории
    // =====================================================
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalTests = await _context.LabTests.CountAsync();
        var completedTests = await _context.LabTests.CountAsync(t => t.Status == "completed");
        var approved = await _context.LabTests.CountAsync(t => t.Result == "approved");
        var rejected = await _context.LabTests.CountAsync(t => t.Result == "rejected");
        var pending = await _context.LabTests.CountAsync(t => t.Status == "in_progress");
        var pendingDecision = await _context.LabTests.CountAsync(t => t.Status == "completed" && t.Result == null);

        var pendingRawMaterials = await _context.RawMaterialBatches.CountAsync(r => r.LabStatus == "pending");
        var pendingBatches = await _context.ProductionBatches
            .Where(b => b.Status == "completed")
            .CountAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalTests,
                completedTests,
                approved,
                rejected,
                pending,
                pendingDecision,
                pendingRawMaterials,
                pendingBatches
            }
        });
    }

    // =====================================================
    // Вспомогательный метод: проверка попадания в норму
    // =====================================================
    private bool CheckIfPassed(decimal? normMin, decimal? normMax, decimal? actualValue)
    {
        if (actualValue == null) return false;
        if (normMin.HasValue && actualValue < normMin.Value) return false;
        if (normMax.HasValue && actualValue > normMax.Value) return false;
        return true;
    }
}