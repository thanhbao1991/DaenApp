using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;
using TraSuaAppWeb.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Logging gọn
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// DbContext (nếu web bạn có dùng)

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// API base url
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:7132/";

// HttpClient gọi API + gắn Bearer từ cookie
builder.Services.AddTransient<ApiAuthHeaderHandler>();
builder.Services.AddHttpClient("Api", c =>
{
    c.BaseAddress = new Uri(apiBaseUrl);
    c.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<ApiAuthHeaderHandler>();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// Global error -> Discord
AppDomain.CurrentDomain.UnhandledException += async (_, e) =>
{
    if (e.ExceptionObject is Exception ex)
        await DiscordService.SendAsync(DiscordEventType.Admin, $"🟟 **Web UnhandledException**\n```{ex}```");
};
TaskScheduler.UnobservedTaskException += async (_, e) =>
{
    if (e.Exception != null)
        await DiscordService.SendAsync(DiscordEventType.Admin, $"⚠️ **Web UnobservedTaskException**\n```{e.Exception}```");
};

// Pipeline
// if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

app.Run();