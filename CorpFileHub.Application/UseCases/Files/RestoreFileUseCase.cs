using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Application.Services;
using System.IO;

namespace CorpFileHub.Application.UseCases.Files
{
    public class RestoreFileUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IAccessControlService _accessControlService;

        public RestoreFileUseCase(
            IFileRepository fileRepository,
            IFileStorageService fileStorageService,
            IYandexDiskService yandexDiskService,
            IAuditLogRepository auditLogRepository,
            IAccessControlService accessControlService)
        {
            _fileRepository = fileRepository;
            _fileStorageService = fileStorageService;
            _yandexDiskService = yandexDiskService;
            _auditLogRepository = auditLogRepository;
            _accessControlService = accessControlService;
        }

        public async Task<bool> ExecuteAsync(int fileId, int userId)
        {
            // Получаем файл, включая удаленные
            var file = await _fileRepository.GetByIdIncludingDeletedAsync(fileId);
            if (file == null || !file.IsDeleted)
                return false;

            var canRestore = await _accessControlService.CanDeleteFileAsync(fileId, userId);
            if (!canRestore)
                throw new UnauthorizedAccessException("Недостаточно прав для восстановления файла");

            // Получаем последнюю версию
            var lastVersion = file.Versions.OrderByDescending(v => v.Version).FirstOrDefault();
            if (lastVersion == null)
                return false;

            // Загружаем файл на Яндекс.Диск
            using var versionStream = await _fileStorageService.GetFileVersionAsync(fileId, lastVersion.Version);
            var yandexPath = await _yandexDiskService.UploadFileAsync(versionStream, file.Name, Path.GetDirectoryName(file.YandexDiskPath) ?? string.Empty);

            file.IsDeleted = false;
            file.Status = FileStatus.Active;
            file.YandexDiskPath = yandexPath;
            file.UpdatedAt = DateTime.UtcNow;

            await _fileRepository.UpdateAsync(file);

            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.FileRestore,
                EntityType = "File",
                EntityId = fileId,
                EntityName = file.Name,
                Description = "Файл восстановлен из архива"
            });

            return true;
        }
    }
}
