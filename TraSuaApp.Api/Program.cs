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

// 🟟 Add Controller & JSON config
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🟟 Add AutoMapper & App Services
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddInfrastructureServices();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

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
builder.Services.AddAuthorization();
builder.Services.AddSignalR();
var app = builder.Build();
app.MapHub<SignalRHub>("/hub/entity");
app.UseMiddleware<LogMiddleware>(); // 🟟 Gọi Middleware tại đây

// 🟟 Swagger for development
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

// 🟟 Middleware pipeline
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();