using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Api.Middleware;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ⚡ Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// 🟟 Đọc Api BaseUrl
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

// 🟟 Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder =
            System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

builder.Services.AddControllers(options =>
{
    options.Filters.Add<TraSuaApp.Api.Filters.ApiExceptionFilter>();
});

// 🟟 DbContext (không pooling; nếu muốn pooling, đổi sang AddDbContextPool)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        opt => opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// 🟟 Memory Cache cho IMemoryCache (bắt buộc cho service của bạn)
builder.Services.AddMemoryCache();

// 🟟 Đăng ký DI cho services (nếu chưa được AddInfrastructureServices thêm sẵn)
builder.Services.AddInfrastructureServices();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Nếu IPhuongThucThanhToanService chưa được đăng ký trong AddInfrastructureServices, mở dòng dưới:
// builder.Services.AddScoped<IPhuongThucThanhToanService, PhuongThucThanhToanService>();

// 🟟 HttpClient gọi ra ngoài (nếu cần)
if (!string.IsNullOrEmpty(apiBaseUrl))
{
    builder.Services.AddHttpClient("Api", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
}

// 🟟 JWT Auth
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
builder.Services.AddAuthorization();

// 🟟 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                "http://www.denncoffee.uk:7130",
                "http://localhost:7130",
                "http://www.denncoffee.uk:7131",
                "http://localhost:7131",
                "http://www.denncoffee.uk:7132",
                "http://localhost:7132"
            )
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddSignalR();

var app = builder.Build();

// ---------------------- Pipeline ----------------------

app.UseMiddleware<LogMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

// ❌ Không dùng HTTPS redirection khi chỉ chạy HTTP

app.UseCors("AllowFrontend");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SignalRHub>("/hub/entity");

app.MapGet("/", () => Results.Ok(new { status = "Backend API running" }))
   .WithMetadata(new HttpMethodMetadata(new[] { "GET", "HEAD" }));

app.Run();