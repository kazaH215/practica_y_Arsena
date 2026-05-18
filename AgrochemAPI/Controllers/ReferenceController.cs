using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgrochemAPI.Data;

namespace AgrochemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReferenceController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReferenceController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // ПРОДУКТЫ
    // =====================================================

    // GET: api/reference/products - получить все продукты
    [HttpGet("products")]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _context.Products.ToListAsync();
        return Ok(new { success = true, count = products.Count, data = products });
    }

    // GET: api/reference/products/active - получить только активные продукты
    [HttpGet("products/active")]
    public async Task<IActionResult> GetActiveProducts()
    {
        var products = await _context.Products
            .Where(p => p.Status == "active")
            .ToListAsync();
        return Ok(new { success = true, count = products.Count, data = products });
    }

    // GET: api/reference/products/{id} - получить продукт по ID
    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { success = false, message = $"Продукт с ID {id} не найден" });

        return Ok(new { success = true, data = product });
    }

    // =====================================================
    // СЫРЬЁ
    // =====================================================

    // GET: api/reference/raw-materials - получить всё сырьё
    [HttpGet("raw-materials")]
    public async Task<IActionResult> GetAllRawMaterials()
    {
        var materials = await _context.RawMaterials.ToListAsync();
        return Ok(new { success = true, count = materials.Count, data = materials });
    }

    // GET: api/reference/raw-materials/{id} - получить сырьё по ID
    [HttpGet("raw-materials/{id}")]
    public async Task<IActionResult> GetRawMaterialById(int id)
    {
        var material = await _context.RawMaterials.FindAsync(id);
        if (material == null)
            return NotFound(new { success = false, message = $"Сырьё с ID {id} не найдено" });

        return Ok(new { success = true, data = material });
    }

    // GET: api/reference/raw-materials/category/{category} - получить по категории
    [HttpGet("raw-materials/category/{category}")]
    public async Task<IActionResult> GetRawMaterialsByCategory(string category)
    {
        var materials = await _context.RawMaterials
            .Where(m => m.Category == category)
            .ToListAsync();
        return Ok(new { success = true, count = materials.Count, data = materials });
    }

    // =====================================================
    // ОБОРУДОВАНИЕ
    // =====================================================

    // GET: api/reference/equipment - получить всё оборудование
    [HttpGet("equipment")]
    public async Task<IActionResult> GetAllEquipment()
    {
        var equipment = await _context.Equipment.ToListAsync();
        return Ok(new { success = true, count = equipment.Count, data = equipment });
    }

    // GET: api/reference/equipment/active - получить только активное оборудование
    [HttpGet("equipment/active")]
    public async Task<IActionResult> GetActiveEquipment()
    {
        var equipment = await _context.Equipment
            .Where(e => e.Status == "active")
            .ToListAsync();
        return Ok(new { success = true, count = equipment.Count, data = equipment });
    }

    // GET: api/reference/equipment/line/{lineId} - получить по линии
    [HttpGet("equipment/line/{lineId}")]
    public async Task<IActionResult> GetEquipmentByLine(string lineId)
    {
        var equipment = await _context.Equipment
            .Where(e => e.LineId == lineId)
            .ToListAsync();
        return Ok(new { success = true, count = equipment.Count, data = equipment });
    }

    // GET: api/reference/equipment/{id} - получить оборудование по ID
    [HttpGet("equipment/{id}")]
    public async Task<IActionResult> GetEquipmentById(int id)
    {
        var item = await _context.Equipment.FindAsync(id);
        if (item == null)
            return NotFound(new { success = false, message = $"Оборудование с ID {id} не найдено" });

        return Ok(new { success = true, data = item });
    }

    // =====================================================
    // ПОЛЬЗОВАТЕЛИ (только чтение)
    // =====================================================

    // GET: api/reference/users - получить всех пользователей
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new {
                u.Id,
                u.Username,
                u.FullName,
                u.Role,
                u.Email,
                u.Department,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();
        return Ok(new { success = true, count = users.Count, data = users });
    }

    // GET: api/reference/users/active - получить только активных
    [HttpGet("users/active")]
    public async Task<IActionResult> GetActiveUsers()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new {
                u.Id,
                u.Username,
                u.FullName,
                u.Role,
                u.Email,
                u.Department
            })
            .ToListAsync();
        return Ok(new { success = true, count = users.Count, data = users });
    }

    // GET: api/reference/users/role/{role} - получить по роли
    [HttpGet("users/role/{role}")]
    public async Task<IActionResult> GetUsersByRole(string role)
    {
        var users = await _context.Users
            .Where(u => u.Role == role && u.IsActive)
            .Select(u => new {
                u.Id,
                u.Username,
                u.FullName,
                u.Email,
                u.Department
            })
            .ToListAsync();
        return Ok(new { success = true, count = users.Count, data = users });
    }

    // GET: api/reference/users/{id} - получить пользователя по ID
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new {
                u.Id,
                u.Username,
                u.FullName,
                u.Role,
                u.Email,
                u.Phone,
                u.Department,
                u.IsActive,
                u.LastLogin,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { success = false, message = $"Пользователь с ID {id} не найден" });

        return Ok(new { success = true, data = user });
    }

    // =====================================================
    // ДАШБОРД (статистика для главной страницы)
    // =====================================================

    // GET: api/reference/dashboard - сводная статистика
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var activeProducts = await _context.Products.CountAsync(p => p.Status == "active");
        var activeMaterials = await _context.RawMaterials.CountAsync();
        var activeEquipment = await _context.Equipment.CountAsync(e => e.Status == "active");
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
        var runningBatches = await _context.ProductionBatches.CountAsync(b => b.Status == "running");
        var plannedBatches = await _context.ProductionBatches.CountAsync(b => b.Status == "planned");
        var completedBatches = await _context.ProductionBatches.CountAsync(b => b.Status == "completed");

        return Ok(new
        {
            success = true,
            data = new
            {
                products = activeProducts,
                rawMaterials = activeMaterials,
                equipment = activeEquipment,
                users = activeUsers,
                batches = new
                {
                    running = runningBatches,
                    planned = plannedBatches,
                    completed = completedBatches,
                    total = runningBatches + plannedBatches + completedBatches
                }
            }
        });
    }
}