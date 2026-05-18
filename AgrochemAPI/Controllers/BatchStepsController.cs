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
public class BatchStepsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BatchStepsController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/batchsteps/batch/{batchId} - получить все шаги партии
    // =====================================================
    [HttpGet("batch/{batchId}")]
    public async Task<IActionResult> GetStepsByBatch(int batchId)
    {
        var batch = await _context.ProductionBatches.FindAsync(batchId);
        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с ID {batchId} не найдена" });

        // Получаем техкарту через заказ
        var order = await _context.ProductionOrders.FindAsync(batch.OrderId);
        if (order?.TechCardId == null)
            return Ok(new { success = true, message = "У партии нет техкарты", data = new List<BatchStepListDto>() });

        // Получаем все шаги из техкарты
        var allSteps = await _context.TechSteps
            .Where(s => s.TechCardId == order.TechCardId)
            .OrderBy(s => s.OrderNum)
            .ToListAsync();

        // Получаем уже созданные выполнения
        var executions = await _context.BatchStepExecutions
            .Where(e => e.BatchId == batchId)
            .ToDictionaryAsync(e => e.StepId);

        var result = new List<BatchStepListDto>();
        foreach (var step in allSteps)
        {
            var exec = executions.GetValueOrDefault(step.Id);
            result.Add(new BatchStepListDto
            {
                Id = exec?.Id ?? 0,
                StepId = step.Id,
                StepType = step.StepType,
                OrderNum = step.OrderNum,
                IsMandatory = step.IsMandatory,
                Status = exec?.Status ?? "pending",
                StartTime = exec?.StartTime,
                EndTime = exec?.EndTime,
                HasDeviation = false  // TODO: проверить таблицу deviations
            });
        }

        return Ok(new { success = true, batchNumber = batch.BatchNumber, data = result });
    }

    // =====================================================
    // GET: api/batchsteps/{id} - получить шаг выполнения по ID
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var execution = await _context.BatchStepExecutions
            .Include(e => e.Batch)
            .Include(e => e.TechStep)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (execution == null)
            return NotFound(new { success = false, message = $"Шаг выполнения с ID {id} не найден" });

        var result = new BatchStepResponseDto
        {
            Id = execution.Id,
            BatchId = execution.BatchId,
            BatchNumber = execution.Batch?.BatchNumber,
            StepId = execution.StepId,
            StepType = execution.TechStep?.StepType,
            OrderNum = execution.TechStep?.OrderNum ?? 0,
            IsMandatory = execution.TechStep?.IsMandatory ?? true,
            Instructions = execution.TechStep?.Instructions,
            PlannedParams = execution.TechStep?.PlannedParams,
            ActualParams = execution.ActualParams,
            Status = execution.Status,
            StartTime = execution.StartTime,
            EndTime = execution.EndTime
        };

        return Ok(new { success = true, data = result });
    }

    // =====================================================
    // GET: api/batchsteps/batch/{batchId}/current - текущий активный шаг
    // =====================================================
    [HttpGet("batch/{batchId}/current")]
    public async Task<IActionResult> GetCurrentStep(int batchId)
    {
        var currentStep = await _context.BatchStepExecutions
            .Include(e => e.TechStep)
            .FirstOrDefaultAsync(e => e.BatchId == batchId && e.Status == "in_progress");

        if (currentStep == null)
        {
            // Ищем следующий незавершённый шаг
            var nextStep = await _context.BatchStepExecutions
                .Include(e => e.TechStep)
                .FirstOrDefaultAsync(e => e.BatchId == batchId && e.Status == "pending");

            if (nextStep != null)
            {
                return Ok(new
                {
                    success = true,
                    data = new BatchStepResponseDto
                    {
                        Id = nextStep.Id,
                        BatchId = nextStep.BatchId,
                        StepId = nextStep.StepId,
                        StepType = nextStep.TechStep?.StepType,
                        OrderNum = nextStep.TechStep?.OrderNum ?? 0,
                        IsMandatory = nextStep.TechStep?.IsMandatory ?? true,
                        Instructions = nextStep.TechStep?.Instructions,
                        PlannedParams = nextStep.TechStep?.PlannedParams,
                        Status = nextStep.Status
                    },
                    message = "Нет активного шага, следующий шаг ожидает запуска"
                });
            }

            return Ok(new { success = true, message = "Все шаги завершены", data = (object?)null });
        }

        var result = new BatchStepResponseDto
        {
            Id = currentStep.Id,
            BatchId = currentStep.BatchId,
            StepId = currentStep.StepId,
            StepType = currentStep.TechStep?.StepType,
            OrderNum = currentStep.TechStep?.OrderNum ?? 0,
            IsMandatory = currentStep.TechStep?.IsMandatory ?? true,
            Instructions = currentStep.TechStep?.Instructions,
            PlannedParams = currentStep.TechStep?.PlannedParams,
            ActualParams = currentStep.ActualParams,
            Status = currentStep.Status,
            StartTime = currentStep.StartTime
        };

        return Ok(new { success = true, data = result });
    }

    // =====================================================
    // POST: api/batchsteps/start - запустить шаг
    // =====================================================
    [HttpPost("start")]
    public async Task<IActionResult> StartStep([FromBody] StartStepDto startDto)
    {
        // Проверяем партию
        var batch = await _context.ProductionBatches.FindAsync(startDto.BatchId);
        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с ID {startDto.BatchId} не найдена" });

        if (batch.Status != "running")
            return BadRequest(new { success = false, message = "Партия не запущена" });

        // Проверяем шаг
        var step = await _context.TechSteps.FindAsync(startDto.StepId);
        if (step == null)
            return NotFound(new { success = false, message = $"Шаг с ID {startDto.StepId} не найден" });

        // Проверяем, есть ли уже выполнение
        var existing = await _context.BatchStepExecutions
            .FirstOrDefaultAsync(e => e.BatchId == startDto.BatchId && e.StepId == startDto.StepId);

        if (existing != null && existing.Status == "completed")
            return BadRequest(new { success = false, message = "Шаг уже завершён" });

        if (existing != null && existing.Status == "in_progress")
            return BadRequest(new { success = false, message = "Шаг уже запущен" });

        // Создаём или обновляем выполнение
        if (existing == null)
        {
            existing = new BatchStepExecution
            {
                BatchId = startDto.BatchId,
                StepId = startDto.StepId,
                Status = "in_progress",
                StartTime = DateTime.Now
            };
            _context.BatchStepExecutions.Add(existing);
        }
        else
        {
            existing.Status = "in_progress";
            existing.StartTime = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Шаг '{step.StepType}' запущен", data = new { executionId = existing.Id } });
    }

    // =====================================================
    // PUT: api/batchsteps/params - обновить параметры шага (без завершения)
    // =====================================================
    [HttpPut("params")]
    public async Task<IActionResult> UpdateParams([FromBody] UpdateStepParamsDto updateDto)
    {
        var execution = await _context.BatchStepExecutions
            .Include(e => e.TechStep)
            .FirstOrDefaultAsync(e => e.Id == updateDto.ExecutionId);

        if (execution == null)
            return NotFound(new { success = false, message = $"Шаг выполнения с ID {updateDto.ExecutionId} не найден" });

        execution.ActualParams = updateDto.ActualParams;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Параметры сохранены", data = execution });
    }

    // =====================================================
    // POST: api/batchsteps/complete - завершить шаг
    // =====================================================
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteStep([FromBody] CompleteStepDto completeDto)
    {
        var execution = await _context.BatchStepExecutions
            .Include(e => e.TechStep)
            .FirstOrDefaultAsync(e => e.Id == completeDto.ExecutionId);

        if (execution == null)
            return NotFound(new { success = false, message = $"Шаг выполнения с ID {completeDto.ExecutionId} не найден" });

        if (execution.Status == "completed")
            return BadRequest(new { success = false, message = "Шаг уже завершён" });

        // Проверяем, заполнены ли параметры (если шаг обязательный)
        if (execution.TechStep?.IsMandatory == true && string.IsNullOrEmpty(completeDto.ActualParams))
            return BadRequest(new { success = false, message = "Для обязательного шага необходимо ввести фактические параметры" });

        // Обновляем выполнение
        execution.Status = "completed";
        execution.EndTime = DateTime.Now;
        if (!string.IsNullOrEmpty(completeDto.ActualParams))
            execution.ActualParams = completeDto.ActualParams;

        await _context.SaveChangesAsync();

        // Записываем событие в аудит
        var auditEntry = new AuditLog
        {
            UserId = completeDto.UserId,
            Action = "complete_step",
            EntityType = "batch_step_execution",
            EntityId = execution.Id,
            NewState = $"{{\"status\":\"completed\",\"end_time\":\"{DateTime.Now}\"}}",
            Timestamp = DateTime.Now
        };
        _context.AuditLogs.Add(auditEntry);
        await _context.SaveChangesAsync();

        // Проверяем, все ли шаги выполнены
        var batchId = execution.BatchId;
        var techCardId = await _context.ProductionBatches
            .Where(b => b.Id == batchId)
            .Select(b => b.Order)
            .Select(o => o != null ? o.TechCardId : null)
            .FirstOrDefaultAsync();

        if (techCardId != null)
        {
            var totalSteps = await _context.TechSteps.CountAsync(s => s.TechCardId == techCardId);
            var completedSteps = await _context.BatchStepExecutions
                .CountAsync(e => e.BatchId == batchId && e.Status == "completed");
            var mandatorySteps = await _context.TechSteps.CountAsync(s => s.TechCardId == techCardId && s.IsMandatory);
            var completedMandatory = await _context.BatchStepExecutions
                .Include(e => e.TechStep)
                .Where(e => e.BatchId == batchId && e.Status == "completed" && e.TechStep!.IsMandatory)
                .CountAsync();

            if (completedMandatory == mandatorySteps)
            {
                // Все обязательные шаги выполнены
                return Ok(new { success = true, message = $"Шаг завершён. Все обязательные шаги выполнены! Можно завершать партию.", allStepsCompleted = true, progress = $"{completedSteps}/{totalSteps}" });
            }

            return Ok(new { success = true, message = $"Шаг завершён. Прогресс: {completedSteps}/{totalSteps} шагов", allStepsCompleted = false, progress = $"{completedSteps}/{totalSteps}" });
        }

        return Ok(new { success = true, message = "Шаг завершён" });
    }

    // =====================================================
    // POST: api/batchsteps/skip - пропустить шаг (только необязательный)
    // =====================================================
    [HttpPost("skip")]
    public async Task<IActionResult> SkipStep([FromBody] SkipStepDto skipDto)
    {
        var execution = await _context.BatchStepExecutions
            .Include(e => e.TechStep)
            .FirstOrDefaultAsync(e => e.Id == skipDto.ExecutionId);

        if (execution == null)
            return NotFound(new { success = false, message = $"Шаг выполнения с ID {skipDto.ExecutionId} не найден" });

        if (execution.TechStep?.IsMandatory == true)
            return BadRequest(new { success = false, message = "Нельзя пропустить обязательный шаг" });

        if (execution.Status == "completed")
            return BadRequest(new { success = false, message = "Шаг уже завершён" });

        execution.Status = "skipped";
        execution.EndTime = DateTime.Now;
        execution.ActualParams = $"{{\"reason\":\"{skipDto.Reason}\", \"skipped_by\":{skipDto.UserId}}}";

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Шаг пропущен" });
    }

    // =====================================================
    // GET: api/batchsteps/batch/{batchId}/progress - прогресс выполнения партии
    // =====================================================
    [HttpGet("batch/{batchId}/progress")]
    public async Task<IActionResult> GetProgress(int batchId)
    {
        var batch = await _context.ProductionBatches.FindAsync(batchId);
        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с ID {batchId} не найдена" });

        var order = await _context.ProductionOrders.FindAsync(batch.OrderId);
        if (order?.TechCardId == null)
            return Ok(new { success = true, data = new { totalSteps = 0, completedSteps = 0, skippedSteps = 0, inProgressSteps = 0, progressPercent = 0 } });

        var totalSteps = await _context.TechSteps.CountAsync(s => s.TechCardId == order.TechCardId);
        var mandatorySteps = await _context.TechSteps.CountAsync(s => s.TechCardId == order.TechCardId && s.IsMandatory);

        var executions = await _context.BatchStepExecutions
            .Where(e => e.BatchId == batchId)
            .ToListAsync();

        var completedSteps = executions.Count(e => e.Status == "completed");
        var skippedSteps = executions.Count(e => e.Status == "skipped");
        var inProgressSteps = executions.Count(e => e.Status == "in_progress");
        var pendingSteps = totalSteps - (completedSteps + skippedSteps + inProgressSteps);

        var completedMandatory = executions
            .Join(_context.TechSteps, e => e.StepId, s => s.Id, (e, s) => new { e, s })
            .Count(x => x.e.Status == "completed" && x.s.IsMandatory);

        var progressPercent = totalSteps > 0 ? (completedSteps + skippedSteps) * 100 / totalSteps : 0;

        return Ok(new
        {
            success = true,
            data = new
            {
                totalSteps,
                mandatorySteps,
                completedSteps,
                skippedSteps,
                inProgressSteps,
                pendingSteps,
                completedMandatory,
                mandatoryCompleted = completedMandatory == mandatorySteps,
                progressPercent
            }
        });
    }
}