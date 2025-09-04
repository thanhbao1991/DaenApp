using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Api.Middleware;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Data;
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
builder.Services.AddAutoMapper(typeof(Program));
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
    opt.RequireHttpsMetadata = true;   // 🟟 bật HTTPS khi production
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

// 🟟 CORS (cho phép WPF client gọi API từ ngoài)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddSignalR();

var app = builder.Build();

// ---------------------- Pipeline ----------------------

// 🟟 Dùng middleware custom
app.UseMiddleware<LogMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error"); // xử lý exception chung
    app.UseHsts();                     // thêm HSTS cho HTTPS
}

// 🟟 Redirect HTTP -> HTTPS
app.UseHttpsRedirection();

// 🟟 CORS cho client
app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapHub<SignalRHub>("/hub/entity");

app.MapGet("/", () => Results.Json(new { status = "Backend API running" }));

app.Run();