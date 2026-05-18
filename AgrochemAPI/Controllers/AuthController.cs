using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgrochemAPI.Data;
using AgrochemAPI.Models;

namespace AgrochemAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // =====================================================
    // POST: api/auth/login - вход в систему
    // =====================================================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // Ищем пользователя в БД
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);

        if (user == null)
            return Unauthorized(new { success = false, message = "Неверный логин или пароль" });

        // ВРЕМЕННО: простой пароль (потом заменим на BCrypt)
        if (loginDto.Password != "123")
            return Unauthorized(new { success = false, message = "Неверный логин или пароль" });

        // Генерируем JWT-токен
        var token = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            Token = token
        });
    }

    // =====================================================
    // POST: api/auth/register - регистрация нового пользователя
    // =====================================================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        // Проверяем, существует ли пользователь с таким логином
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == registerDto.Username);

        if (existingUser != null)
            return BadRequest(new { success = false, message = "Пользователь с таким логином уже существует" });

        // Проверяем, существует ли пользователь с такой почтой
        var existingEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

        if (existingEmail != null)
            return BadRequest(new { success = false, message = "Пользователь с такой почтой уже существует" });

        // Создаём нового пользователя
        var user = new User
        {
            Username = registerDto.Username,
            FullName = registerDto.FullName,
            Email = registerDto.Email,
            Role = registerDto.Role,
            Department = registerDto.Department,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Генерируем токен для нового пользователя
        var token = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            Token = token
        });
    }

    // =====================================================
    // Генерация JWT-токена
    // =====================================================
    private string GenerateJwtToken(User user)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "SuperSecretKeyForAgrochemSystem2025!");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

// =====================================================
// DTO (Data Transfer Objects) - классы для передачи данных
// =====================================================

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "operator";
    public string Department { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}