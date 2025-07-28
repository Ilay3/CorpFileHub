using CorpFileHub.Infrastructure.Data;
using CorpFileHub.Infrastructure;
using CorpFileHub.Application;
using CorpFileHub.Presentation.Components;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ��������� Serilog ��� �����������
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("./Logs/corpfilehub-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ���������� DbContext ��� PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ����������� �������� ����������
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ��������� ����������� ��� ������������
builder.Services.AddControllers();

var app = builder.Build();

// �������� ����������� �����
try
{
    var archivePath = builder.Configuration["FileStorage:ArchivePath"] ?? "./Archive";
    var logsPath = "./Logs";

    if (!Directory.Exists(archivePath))
    {
        Directory.CreateDirectory(archivePath);
        Log.Information($"������� ����� ������: {archivePath}");
    }

    if (!Directory.Exists(logsPath))
    {
        Directory.CreateDirectory(logsPath);
        Log.Information($"������� ����� �����: {logsPath}");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "������ �������� ����� ��� �������");
}

// ���������� �������� �� ��� ������� (��� ����������)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        Log.Information("���� ������ ���������/�������");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "������ ������������� ���� ������");
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

// ��������� �������� ��� ������������
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Log.Information("CorpFileHub ������� �������");

app.Run();