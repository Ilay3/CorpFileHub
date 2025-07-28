using CorpFileHub.Infrastructure.Data;
using CorpFileHub.Infrastructure;
using CorpFileHub.Application;
using CorpFileHub.Presentation.Components;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog для логирования
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("./Logs/corpfilehub-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление DbContext для PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Регистрация сервисов приложения
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Добавляем контроллеры для тестирования
builder.Services.AddControllers();

var app = builder.Build();

// Создание необходимых папок
try
{
    var archivePath = builder.Configuration["FileStorage:ArchivePath"] ?? "./Archive";
    var logsPath = "./Logs";

    if (!Directory.Exists(archivePath))
    {
        Directory.CreateDirectory(archivePath);
        Log.Information($"Создана папка архива: {archivePath}");
    }

    if (!Directory.Exists(logsPath))
    {
        Directory.CreateDirectory(logsPath);
        Log.Information($"Создана папка логов: {logsPath}");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Ошибка создания папок при запуске");
}

// Применение миграций БД при запуске (для разработки)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        Log.Information("База данных проверена/создана");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Ошибка инициализации базы данных");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// Добавляем маршруты для контроллеров
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Log.Information("CorpFileHub запущен успешно");

app.Run();