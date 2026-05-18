using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgrochemAPI.Data;
using AgrochemAPI.DTOs;
using AgrochemAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AgrochemAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> Authenticate(LoginDto loginDto);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto?> Authenticate(LoginDto loginDto)
    {
        // Ищем пользователя в БД
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);

        if (user == null)
            return null;

        // ВРЕМЕННО: простой пароль (потом заменим на BCrypt)
        // В твоей БД нет поля password, поэтому пока заглушка
        if (loginDto.Password != "123")
            return null;

        // Генерируем JWT-токен
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            Token = token
        };
    }

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