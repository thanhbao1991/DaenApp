using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Logging;
using TraSuaAppWeb.Data;
using TraSuaAppWeb.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình dịch vụ
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();


builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 🟟 Bind DiscordWebhookOptions & Init logger
var webhookOptions = new DiscordWebhookOptions();
builder.Configuration.GetSection("Discord").Bind(webhookOptions);
DiscordLogger.Init(webhookOptions);


var app = builder.Build();

// ======================
// BẮT LỖI TOÀN CỤC
// ======================
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var ex = e.ExceptionObject as Exception;
    ErrorLogger.LogSync(ex?.ToString());
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    ErrorLogger.LogSync(e.Exception.ToString());
};

// ======================
// Middleware
// ======================

// ⚠️ Tạm tắt HTTPS nếu chưa dùng SSL thật
// if (!app.Environment.IsDevelopment())
//     app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<HoaDonHub>("/hoadonhub");

app.Run();