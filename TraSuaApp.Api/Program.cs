using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Api.Middleware;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 🟟 Add Controllers + JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

builder.Services.AddControllers(options =>
{
    options.Filters.Add<TraSuaApp.Api.Filters.ApiExceptionFilter>();
});

// 🟟 Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        opt => opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// 🟟 Add AutoMapper & Services
builder.Services.AddInfrastructureServices();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// 🟟 JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "");
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false;   // 🟟 tắt bắt buộc HTTPS
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
builder.Services.AddAuthorization();

// 🟟 CORS (cho phép web chạy thật + localhost)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                "http://www.denncoffee.uk:7131", // web chạy domain
                "http://localhost:7131"          // chạy dev local
            )
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddSignalR();
builder.Services.AddSignalR();

// 🟟 Lắng nghe port 80
//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(80);
//});

var app = builder.Build();

// ---------------------- Pipeline ----------------------

// 🟟 Dùng middleware custom
app.UseMiddleware<LogMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

// ❌ Không dùng HTTPS redirection khi chỉ chạy HTTP

// 🟟 CORS cho client
app.UseCors("AllowFrontend");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SignalRHub>("/hub/entity");

app.MapGet("/", () => Results.Json(new { status = "Backend API running" }));

app.Run();