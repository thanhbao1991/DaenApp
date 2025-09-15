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

// 🟟 Đăng ký HttpClient cho API backend
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri("http://api.denncoffee.uk");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});



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