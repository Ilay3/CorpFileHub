using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace CorpFileHub.Application.Services
{
    public class AuditCleanupService : BackgroundService
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditCleanupService> _logger;
        private readonly int _retentionDays;

        public AuditCleanupService(IAuditService auditService, ILogger<AuditCleanupService> logger, IConfiguration configuration)
        {
            _auditService = auditService;
            _logger = logger;
            _retentionDays = configuration.GetSection("Audit").GetValue<int>("RetentionDays", 365);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Запуск очистки журнала аудита");
                await _auditService.CleanupOldLogsAsync(_retentionDays);

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

