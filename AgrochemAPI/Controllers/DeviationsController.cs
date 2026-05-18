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
public class DeviationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DeviationsController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // DEVIATIONS
    // =====================================================

    // GET: api/deviations - получить все отклонения
    [HttpGet]
    public async Task<IActionResult> GetAllDeviations()
    {
        var deviations = await _context.Deviations
            .Include(d => d.Batch)
            .Include(d => d.TechStep)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DeviationListDto
            {
                Id = d.Id,
                BatchId = d.BatchId,
                BatchNumber = d.Batch != null ? d.Batch.BatchNumber : null,
                StepId = d.StepId,
                StepType = d.TechStep != null ? d.TechStep.StepType : null,
                Parameter = d.Parameter,
                PlannedValue = d.PlannedValue,
                ActualValue = d.ActualValue,
                Severity = d.Severity,
                Comment = d.Comment,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, count = deviations.Count, data = deviations });
    }

    // GET: api/deviations/{id} - получить отклонение по ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeviationById(int id)
    {
        var deviation = await _context.Deviations
            .Include(d => d.Batch)
                .ThenInclude(b => b!.Order)
                .ThenInclude(o => o!.Product)
            .Include(d => d.TechStep)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (deviation == null)
            return NotFound(new { success = false, message = $"Отклонение с ID {id} не найдено" });

        var result = new DeviationDetailDto
        {
            Id = deviation.Id,
            BatchId = deviation.BatchId,
            BatchNumber = deviation.Batch?.BatchNumber,
            ProductName = deviation.Batch?.Order?.Product?.Name,
            StepId = deviation.StepId,
            StepType = deviation.TechStep?.StepType,
            StepOrderNum = deviation.TechStep?.OrderNum,
            Parameter = deviation.Parameter,
            PlannedValue = deviation.PlannedValue,
            ActualValue = deviation.ActualValue,
            Severity = deviation.Severity,
            Comment = deviation.Comment,
            CreatedAt = deviation.CreatedAt
        };

        return Ok(new { success = true, data = result });
    }

    // GET: api/deviations/batch/{batchId} - отклонения по партии
    [HttpGet("batch/{batchId}")]
    public async Task<IActionResult> GetDeviationsByBatch(int batchId)
    {
        var batch = await _context.ProductionBatches.FindAsync(batchId);
        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с ID {batchId} не найдена" });

        var deviations = await _context.Deviations
            .Where(d => d.BatchId == batchId)
            .Include(d => d.TechStep)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DeviationListDto
            {
                Id = d.Id,
                BatchId = d.BatchId,
                StepId = d.StepId,
                StepType = d.TechStep != null ? d.TechStep.StepType : null,
                Parameter = d.Parameter,
                PlannedValue = d.PlannedValue,
                ActualValue = d.ActualValue,
                Severity = d.Severity,
                Comment = d.Comment,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, batchNumber = batch.BatchNumber, count = deviations.Count, data = deviations });
    }

    // GET: api/deviations/severity/{severity} - отклонения по уровню серьёзности
    [HttpGet("severity/{severity}")]
    public async Task<IActionResult> GetDeviationsBySeverity(string severity)
    {
        var validSeverities = new[] { "info", "warning", "critical" };
        if (!validSeverities.Contains(severity))
            return BadRequest(new { success = false, message = $"Неверный уровень. Допустимые: {string.Join(", ", validSeverities)}" });

        var deviations = await _context.Deviations
            .Where(d => d.Severity == severity)
            .Include(d => d.Batch)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DeviationListDto
            {
                Id = d.Id,
                BatchId = d.BatchId,
                BatchNumber = d.Batch != null ? d.Batch.BatchNumber : null,
                Parameter = d.Parameter,
                ActualValue = d.ActualValue,
                Severity = d.Severity,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, severity, count = deviations.Count, data = deviations });
    }

    // POST: api/deviations - создать отклонение
    [HttpPost]
    public async Task<IActionResult> CreateDeviation([FromBody] CreateDeviationDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Неверные данные", errors = ModelState });

        // Проверяем партию
        var batch = await _context.ProductionBatches.FindAsync(createDto.BatchId);
        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с ID {createDto.BatchId} не найдена" });

        // Если указан step_id, проверяем его
        if (createDto.StepId.HasValue)
        {
            var step = await _context.TechSteps.FindAsync(createDto.StepId.Value);
            if (step == null)
                return NotFound(new { success = false, message = $"Шаг с ID {createDto.StepId} не найден" });
        }

        var deviation = new Deviation
        {
            BatchId = createDto.BatchId,
            StepId = createDto.StepId,
            Parameter = createDto.Parameter,
            PlannedValue = createDto.PlannedValue,
            ActualValue = createDto.ActualValue,
            Severity = createDto.Severity,
            Comment = createDto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Deviations.Add(deviation);
        await _context.SaveChangesAsync();

        // Если отклонение критическое - создаём уведомление для технолога
        if (createDto.Severity == "critical")
        {
            await CreateNotificationForTechnologists($"Критическое отклонение в партии {batch.BatchNumber}",
                $"Параметр: {createDto.Parameter}, Факт: {createDto.ActualValue}, План: {createDto.PlannedValue ?? "не задан"}",
                "critical", "production_batch", batch.Id);
        }

        // Записываем в аудит
        var audit = new AuditLog
        {
            UserId = createDto.UserId,
            Action = "create_deviation",
            EntityType = "deviation",
            EntityId = deviation.Id,
            NewState = $"{{\"parameter\":\"{createDto.Parameter}\",\"severity\":\"{createDto.Severity}\"}}",
            Timestamp = DateTime.Now
        };
        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDeviationById), new { id = deviation.Id },
            new { success = true, message = "Отклонение зафиксировано", data = deviation });
    }

    // GET: api/deviations/statistics - статистика отклонений
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var total = await _context.Deviations.CountAsync();
        var critical = await _context.Deviations.CountAsync(d => d.Severity == "critical");
        var warning = await _context.Deviations.CountAsync(d => d.Severity == "warning");
        var info = await _context.Deviations.CountAsync(d => d.Severity == "info");

        var batchesWithDeviations = await _context.Deviations
            .Select(d => d.BatchId)
            .Distinct()
            .CountAsync();

        // Топ-5 параметров с отклонениями
        var topParameters = await _context.Deviations
            .GroupBy(d => d.Parameter)
            .Select(g => new TopDeviationParameterDto
            {
                Parameter = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(p => p.Count)
            .Take(5)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new DeviationStatsDto
            {
                Total = total,
                Critical = critical,
                Warning = warning,
                Info = info,
                BatchesWithDeviations = batchesWithDeviations,
                TopParameters = topParameters
            }
        });
    }

    // =====================================================
    // NOTIFICATIONS
    // =====================================================

    // GET: api/deviations/notifications/user/{userId} - уведомления пользователя
    [HttpGet("notifications/user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(int userId, [FromQuery] bool unreadOnly = false)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationListDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                EntityType = n.EntityType,
                EntityId = n.EntityId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        return Ok(new { success = true, unreadCount, count = notifications.Count, data = notifications });
    }

    // GET: api/deviations/notifications/unread/count/{userId} - количество непрочитанных
    [HttpGet("notifications/unread/count/{userId}")]
    public async Task<IActionResult> GetUnreadCount(int userId)
    {
        var count = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        return Ok(new { success = true, userId, unreadCount = count });
    }

    // POST: api/deviations/notifications/read - отметить как прочитанное
    [HttpPost("notifications/read")]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkReadDto markDto)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == markDto.NotificationId && n.UserId == markDto.UserId);

        if (notification == null)
            return NotFound(new { success = false, message = "Уведомление не найдено" });

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Уведомление отмечено как прочитанное" });
    }

    // POST: api/deviations/notifications/read-all - отметить все как прочитанные
    [HttpPost("notifications/read-all/{userId}")]
    public async Task<IActionResult> MarkAllAsRead(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in notifications)
            n.IsRead = true;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Отмечено {notifications.Count} уведомлений" });
    }

    // =====================================================
    // EVENTS (СОБЫТИЯ)
    // =====================================================

    // GET: api/deviations/events - получить все события
    [HttpGet("events")]
    public async Task<IActionResult> GetAllEvents([FromQuery] int? batchId = null)
    {
        var query = _context.EventLogs
            .Include(e => e.Batch)
            .Include(e => e.User)
            .OrderByDescending(e => e.CreatedAt)
            .AsQueryable();

        if (batchId.HasValue)
            query = query.Where(e => e.BatchId == batchId.Value);

        var events = await query
            .Select(e => new EventListDto
            {
                Id = e.Id,
                BatchId = e.BatchId,
                BatchNumber = e.Batch != null ? e.Batch.BatchNumber : null,
                EventType = e.EventType,
                Description = e.Description,
                UserName = e.User != null ? e.User.FullName : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, count = events.Count, data = events });
    }

    // GET: api/deviations/events/batch/{batchId} - события по партии
    [HttpGet("events/batch/{batchId}")]
    public async Task<IActionResult> GetBatchEvents(int batchId)
    {
        var batch = await _context.ProductionBatches.FindAsync(batchId);
        if (batch == null)
            return NotFound(new { success = false, message = $"Партия с ID {batchId} не найдена" });

        var events = await _context.EventLogs
            .Where(e => e.BatchId == batchId)
            .Include(e => e.User)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EventListDto
            {
                Id = e.Id,
                BatchId = e.BatchId,
                EventType = e.EventType,
                Description = e.Description,
                UserName = e.User != null ? e.User.FullName : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, batchNumber = batch.BatchNumber, count = events.Count, data = events });
    }

    // POST: api/deviations/events - создать событие
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto createDto)
    {
        // Проверяем партию (если указана)
        if (createDto.BatchId.HasValue)
        {
            var batch = await _context.ProductionBatches.FindAsync(createDto.BatchId.Value);
            if (batch == null)
                return NotFound(new { success = false, message = $"Партия с ID {createDto.BatchId} не найдена" });
        }

        var eventLog = new EventLog
        {
            BatchId = createDto.BatchId,
            EventType = createDto.EventType,
            Description = createDto.Description,
            UserId = createDto.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync();

        // В зависимости от типа события, создаём уведомления
        if (createDto.EventType == "batch_started" && createDto.BatchId.HasValue)
        {
            await CreateNotificationForTechnologists("Партия запущена",
                $"Партия запущена и находится в работе", "info", "production_batch", createDto.BatchId.Value);
        }
        else if (createDto.EventType == "batch_completed" && createDto.BatchId.HasValue)
        {
            await CreateNotificationForTechnologists("Партия завершена",
                $"Партия завершена, ожидает лабораторного контроля", "warning", "production_batch", createDto.BatchId.Value);
        }

        return Ok(new { success = true, message = "Событие зафиксировано", data = eventLog });
    }

    // =====================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // =====================================================

    private async Task CreateNotificationForTechnologists(string title, string message, string type, string entityType, int entityId)
    {
        // Получаем всех технологов
        var technologists = await _context.Users
            .Where(u => u.Role == "technologist" && u.IsActive)
            .ToListAsync();

        foreach (var tech in technologists)
        {
            var notification = new Notification
            {
                UserId = tech.Id,
                Title = title,
                Message = message,
                Type = type,
                EntityType = entityType,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }

        // Также уведомляем shift_supervisor и admin
        var supervisors = await _context.Users
            .Where(u => (u.Role == "shift_supervisor" || u.Role == "admin") && u.IsActive)
            .ToListAsync();

        foreach (var sup in supervisors)
        {
            var notification = new Notification
            {
                UserId = sup.Id,
                Title = title,
                Message = message,
                Type = type,
                EntityType = entityType,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();
    }
}