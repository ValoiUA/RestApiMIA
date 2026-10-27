using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using MyTodoApi.DTO;
using MyTodoApi.Models;
using MyTodoApi.Repositories;
using MyTodoApi.Services;

namespace MyTodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("0. Authentication")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IUserRepository userRepository,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [EndpointSummary("Реєстрація нового користувача")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { Message = "Email та пароль є обов'язковими." });
        }

        var normalizedEmail = dto.Email.ToLower().Trim();
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            return BadRequest(new { Message = "Користувач із таким email вже існує." });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var role = string.Equals(dto.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
            ? UserRoles.Admin
            : UserRoles.User;

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Role = role
        };

        await _userRepository.CreateAsync(user);

        return Ok(new { Message = "Користувача успішно зареєстровано.", Email = user.Email, Role = user.Role });
    }

    [HttpPost("login")]
    [EndpointSummary("Вхід користувача й отримання токенів")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        var normalizedEmail = dto.Email.ToLower().Trim();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Невірний email або пароль." });
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshDays = double.Parse(_configuration["Jwt:RefreshTokenDurationDays"] ?? "7");
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDays);

        await _userRepository.UpdateAsync(user.Id!, user);

        return Ok(new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("refresh")]
    [EndpointSummary("Оновлення токенів за допомогою refresh token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return BadRequest(new { Message = "Необхідно передати refresh token." });
        }

        var users = await _userRepository.FindAsync(u => u.RefreshToken == dto.RefreshToken);
        var user = users.FirstOrDefault();

        if (user is null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(new { Message = "Недійсний або прострочений refresh token." });
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshDays = double.Parse(_configuration["Jwt:RefreshTokenDurationDays"] ?? "7");
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDays);

        await _userRepository.UpdateAsync(user.Id!, user);

        return Ok(new AuthResponseDTO
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
}