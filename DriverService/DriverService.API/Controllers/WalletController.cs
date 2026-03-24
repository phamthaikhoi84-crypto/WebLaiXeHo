using DriverService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DriverService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly AppDbContext _context;

    public WalletController(AppDbContext context) { _context = context; }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = Guid.Parse(User.FindFirstValue("id"));
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();
        return Ok(new { Balance = user.Balance });
    }

    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue("id"));
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Cộng tiền vào ví
        user.Balance += request.Amount;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Nạp tiền thành công!", NewBalance = user.Balance });
    }
}
public record TopUpRequest(decimal Amount);