using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace CorpFileHub.Application.Services
{
    public class FileVersionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileVersionCleanupService> _logger;
        private readonly int _retentionDays;
        private readonly int _maxVersions;

        public FileVersionCleanupService(IServiceScopeFactory scopeFactory,
            ILogger<FileVersionCleanupService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _retentionDays = configuration.GetValue<int>("Versioning:RetentionDays", 365);
            _maxVersions = configuration.GetValue<int>("Versioning:MaxVersionsPerFile", 10);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var fileManagementService = scope.ServiceProvider.GetRequiredService<IFileManagementService>();

                    var removed = await fileManagementService.CleanupOldVersionsAsync();
                    if (removed > 0)
                    {
                        _logger.LogInformation("Очистка старых версий файлов: удалено {Count}", removed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка фона при очистке версий");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
