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
    // 1. Thêm API Đăng Ký
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var isExist = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (isExist) return BadRequest("Email này đã được sử dụng!");

        var newUser = new DriverService.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = request.Password,
            Role = request.Role
        };
        _context.Users.Add(newUser);

        // Tự động tạo hồ sơ dựa trên thông tin chọn từ Form
        if (request.Role == "Driver")
        {
            var newDriver = new DriverService.Domain.Entities.Driver
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                // Lấy thông tin bằng lái từ Client gửi lên (Nếu không có thì mặc định gán B2)
                LicenseType = request.LicenseType.HasValue
                    ? (DriverService.Domain.Enums.LicenseType)request.LicenseType.Value
                    : DriverService.Domain.Enums.LicenseType.B2
            };
            _context.Drivers.Add(newDriver);
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Đăng ký thành công!" });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return Unauthorized("Email không tồn tại");

        // ✅ BỔ SUNG ĐOẠN NÀY ĐỂ KIỂM TRA MẬT KHẨU
        if (user.PasswordHash != request.Password)
            return Unauthorized("Mật khẩu không chính xác!");

        var token = _authService.GenerateToken(user.Email, user.Role, user.Id);
        return Ok(new { Token = token, Role = user.Role });
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterDto(string Email, string Password, string Role, int? LicenseType);