using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace CorpFileHub.Application.Services
{
    public class AuditCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditCleanupService> _logger;
        private readonly int _retentionDays;

        public AuditCleanupService(IServiceScopeFactory scopeFactory, ILogger<AuditCleanupService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _retentionDays = configuration.GetSection("Audit").GetValue<int>("RetentionDays", 365);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

                _logger.LogInformation("Запуск очистки журнала аудита");
                await auditService.CleanupOldLogsAsync(_retentionDays);

                try
                {
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }
    }
}

