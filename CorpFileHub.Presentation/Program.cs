using CorpFileHub.Application;
using CorpFileHub.Infrastructure;
using CorpFileHub.Infrastructure.Data;
using CorpFileHub.Presentation.Hubs;
using Microsoft.EntityFrameworkCore;
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

// SignalR для real-time обновлений
builder.Services.AddSignalR();

// Контроллеры для API
builder.Services.AddControllers();
builder.Services.AddHttpClient();

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
