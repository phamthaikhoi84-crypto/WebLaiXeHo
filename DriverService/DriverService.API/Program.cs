using DriverService.API.Hubs;
using DriverService.API.Middleware;
using DriverService.Application.Interfaces;
using DriverService.Infrastructure.Data;
using DriverService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. DI
builder.Services.AddScoped<IRideRepository, RideRepository>();

// 3. SignalR & Controllers
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Ghi đích danh cổng Live Server của bạn vào đây
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
builder.Services.AddControllers();

// 4. JWT Authentication
var jwtKey = "day_la_khoa_bao_mat_rat_dai_va_an_toan_nhat_co_the_12345";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };

    // 👇 ĐÂY LÀ ĐOẠN QUAN TRỌNG NHẤT CHO SIGNALR (Phải giữ lại) 👇
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            // Nếu là request gửi đến Hub SignalR thì lấy token từ query string
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/ride"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
// 5. Swagger với cấu hình JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference{ Type = ReferenceType.SecurityScheme, Id = "Bearer"}
            },
            new string[]{}
        }
    });
});

var app = builder.Build();

// Sử dụng Middleware tự build
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RideHub>("/hubs/ride");

// Đảm bảo Database luôn được tự động cập nhật
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DriverService.Infrastructure.Data.AppDbContext>();
    dbContext.Database.Migrate();

    // ✅ ĐÃ FIX: Tách riêng việc kiểm tra tài khoản Admin. Dù DB có dữ liệu hay chưa, miễn chưa có Admin là nó sẽ tự tạo.
    if (!dbContext.Users.Any(u => u.Email == "admin@test.com"))
    {
        dbContext.Users.Add(new DriverService.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            PasswordHash = "admin123",
            Role = "Admin",
            Balance = 0
        });
        dbContext.SaveChanges();
    }

    // Các tài khoản test khác
    if (!dbContext.Users.Any(u => u.Email == "user@test.com"))
    {
        var customerId = Guid.NewGuid();
        dbContext.Users.Add(new DriverService.Domain.Entities.User { Id = customerId, Email = "user@test.com", PasswordHash = "123", Role = "User", Balance = 0 });

        var driverUserId = Guid.NewGuid();
        dbContext.Users.Add(new DriverService.Domain.Entities.User { Id = driverUserId, Email = "driver@test.com", PasswordHash = "123", Role = "Driver", Balance = 0 });
        dbContext.SaveChanges();

        dbContext.Drivers.Add(new DriverService.Domain.Entities.Driver
        {
            Id = Guid.NewGuid(),
            UserId = driverUserId,
            LicenseType = DriverService.Domain.Enums.LicenseType.B1,
            IsApproved = true // Đã duyệt mẫu người này để test
        });
        dbContext.SaveChanges();
    }
}

app.Run();