using Microsoft.AspNetCore.Mvc;
using DriverService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DriverService.Application.Services.AuthService _authService;

    public AuthController(AppDbContext context)
    {
        _context = context;
        _authService = new DriverService.Application.Services.AuthService();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Demo: Kiểm tra DB (Thực tế phải hash password)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return Unauthorized("Email không tồn tại");

        var token = _authService.GenerateToken(user.Email, user.Role, user.Id);
        return Ok(new { Token = token, Role = user.Role });
    }
}

public record LoginRequest(string Email, string Password);