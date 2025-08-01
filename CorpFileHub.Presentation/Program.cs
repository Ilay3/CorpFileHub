﻿using CorpFileHub.Application;
using CorpFileHub.Infrastructure;
using CorpFileHub.Infrastructure.Data;
using CorpFileHub.Presentation.Hubs;
using CorpFileHub.Presentation;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка логирования Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(
        path: builder.Configuration["Logging:File:Path"] ?? "./Logs/corpfilehub-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление сервисов
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HTTP Context Accessor для аудита
builder.Services.AddHttpContextAccessor();

// Подключение к базе данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация слоев
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPresentationServices();

// SignalR для real-time обновлений
builder.Services.AddSignalR();

// Контроллеры для API
builder.Services.AddControllers();

var ignoreSsl = builder.Configuration.GetValue<bool>("Security:IgnoreInvalidCertificate", false);

builder.Services.AddHttpClient("ServerAPI", (sp, client) =>
{
    var httpContext = sp.GetService<IHttpContextAccessor>()?.HttpContext;
    if (httpContext != null)
    {
        client.BaseAddress = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
    }
    else
    {
        var baseUrl = builder.Configuration["Server:BaseUrl"] ?? builder.Configuration["urls"]?.Split(';').FirstOrDefault() ?? "http://localhost:5275";
        client.BaseAddress = new Uri(baseUrl);
    }

}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (ignoreSsl)
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;

});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

// Хранилище сессий в памяти
builder.Services.AddDistributedMemoryCache();

// Настройка сессий
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>("Security:SessionTimeoutMinutes", 480));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// CORS для API
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Настройка pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    if (builder.Configuration.GetValue<bool>("Security:RequireHttps", false))
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseCors("DefaultPolicy");
app.UseSession();

app.UseAntiforgery();

// Маршруты
app.MapRazorComponents<CorpFileHub.Presentation.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapHub<FileOperationHub>("/fileOperationHub");

// Применение миграций при запуске
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // Создание тестового администратора, если пользователей нет
        if (!context.Users.Any())
        {
            var admin = new CorpFileHub.Domain.Entities.User
            {
                Email = "admin@corp.local",
                FullName = "Test Admin",
                PasswordHash = CorpFileHub.Application.Utilities.PasswordHasher.HashPassword("Admin123!"),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true,
                IsAdmin = true,
                Department = "IT",
                Position = "Administrator"
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            Log.Information("Создан тестовый пользователь администратора: admin@corp.local / Admin123!");
        }

        Log.Information("База данных успешно обновлена");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Ошибка при применении миграций базы данных");
        throw;
    }
}

Log.Information("🚀 CorpFileHub запущен на {Urls}", string.Join(", ", builder.Configuration["urls"]?.Split(';') ?? new[] { "http://localhost:5275" }));

app.Run();
