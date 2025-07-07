using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 🟟 Add controller & JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

// 🟟 Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🟟 Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🟟 Add AutoMapper & App Services
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddInfrastructureServices(); // -> bạn đã định nghĩa hàm này
builder.Services.AddScoped<JwtTokenService>();

// 🟟 Add Authentication & JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "");
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false;
    opt.SaveToken = true;
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// 🟟 Add Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// 🟟 Use Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ===== Middleware pipeline =====
app.UseRouting(); // Optional but helps with custom middleware order

app.UseAuthentication(); // ⬅️ Trước Authorization
app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>(); // Xử lý lỗi toàn cục
app.UseMiddleware<LogMiddleware>();       // Ghi log toàn bộ request/response

app.MapControllers(); // ⬅️ Kết nối Controller với routing

app.Run();