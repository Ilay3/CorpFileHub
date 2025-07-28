using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Files
{
    public class DownloadFileUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IAuditLogRepository _auditLogRepository;

        public DownloadFileUseCase(
            IFileRepository fileRepository,
            IYandexDiskService yandexDiskService,
            IAuditLogRepository auditLogRepository)
        {
            _fileRepository = fileRepository;
            _yandexDiskService = yandexDiskService;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<(Stream fileStream, string fileName, string contentType)> ExecuteAsync(int fileId, int userId)
        {
            // 1. Получаем файл
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.IsDeleted)
                throw new ArgumentException("Файл не найден");

            // 2. TODO: Проверяем права доступа (будет реализовано в AccessControlService)

            // 3. Скачиваем файл с Яндекс.Диска
            var fileStream = await _yandexDiskService.DownloadFileAsync(file.YandexDiskPath);

            // 4. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.FileDownload,
                EntityType = "File",
                EntityId = fileId,
                EntityName = file.Name,
                Description = $"Файл '{file.Name}' скачан"
            });

            return (fileStream, file.Name, file.ContentType);
        }
    }
}