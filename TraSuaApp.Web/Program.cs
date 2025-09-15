using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Config; // ⚡ import Config
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;
using TraSuaAppWeb.Data;

var builder = WebApplication.CreateBuilder(args);

// Service
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(Config.ConnectionString)); // ⚡ dùng ConnectionString từ Config
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();



// Lấy config cho API
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "NOT CONFIGURED";

// In ra console khi start app
Console.WriteLine($"[TraSuaAppWeb] Using API BaseUrl = {apiBaseUrl}");

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});


// Các service khác
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();


builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// ======================
// BẮT LỖI TOÀN CỤC
// ======================
AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
{
    var ex = e.ExceptionObject as Exception;
    if (ex != null)
    {
        await DiscordService.SendAsync(DiscordEventType.Admin, $"🟟 **Web UnhandledException**\n```{ex}```");
    }
};

TaskScheduler.UnobservedTaskException += async (sender, e) =>
{
    if (e.Exception != null)
    {
        await DiscordService.SendAsync(DiscordEventType.Admin, $"⚠️ **Web UnobservedTaskException**\n```{e.Exception}```");
    }
};

// Middleware
// ⚠️ Tạm tắt HTTPS nếu chưa dùng SSL thật
// if (!app.Environment.IsDevelopment())
//     app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

app.Run();