using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CorpFileHub.Application.Services
{
    public class FileEditMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileEditMonitorService> _logger;

        public FileEditMonitorService(
            IServiceScopeFactory scopeFactory,
            ILogger<FileEditMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var fileRepository = scope.ServiceProvider.GetRequiredService<IFileRepository>();
                    var fileManagementService = scope.ServiceProvider.GetRequiredService<IFileManagementService>();
                    var yandexDiskService = scope.ServiceProvider.GetRequiredService<IYandexDiskService>();

                    var editingFiles = await fileRepository.GetByStatusAsync(FileStatus.InEditing);
                    foreach (var file in editingFiles)
                    {
                        var lastVersionTime = file.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault()?.CreatedAt ?? file.CreatedAt;
                        var remoteModified = await yandexDiskService.GetLastModifiedAsync(file.YandexDiskPath);

                        if (remoteModified > lastVersionTime)
                        {
                            _logger.LogInformation("Обнаружено обновление файла {FileId}", file.Id);
                            await fileManagementService.CreateVersionAsync(file.Id, file.OwnerId, "Автоматическая версия после редактирования");
                            await fileManagementService.UnmarkFileAsEditingAsync(file.Id, file.OwnerId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка мониторинга редактируемых файлов");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
