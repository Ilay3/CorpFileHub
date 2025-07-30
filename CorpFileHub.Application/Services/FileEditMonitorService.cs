using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CorpFileHub.Application.Services
{
    public class FileEditMonitorService : BackgroundService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileManagementService _fileManagementService;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly ILogger<FileEditMonitorService> _logger;

        public FileEditMonitorService(
            IFileRepository fileRepository,
            IFileManagementService fileManagementService,
            IYandexDiskService yandexDiskService,
            ILogger<FileEditMonitorService> logger)
        {
            _fileRepository = fileRepository;
            _fileManagementService = fileManagementService;
            _yandexDiskService = yandexDiskService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var editingFiles = await _fileRepository.GetByStatusAsync(FileStatus.InEditing);
                    foreach (var file in editingFiles)
                    {
                        var lastVersionTime = file.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault()?.CreatedAt ?? file.CreatedAt;
                        var remoteModified = await _yandexDiskService.GetLastModifiedAsync(file.YandexDiskPath);

                        if (remoteModified > lastVersionTime)
                        {
                            _logger.LogInformation("Обнаружено обновление файла {FileId}", file.Id);
                            await _fileManagementService.CreateVersionAsync(file.Id, file.OwnerId, "Автоматическая версия после редактирования");
                            await _fileManagementService.UnmarkFileAsEditingAsync(file.Id, file.OwnerId);
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
