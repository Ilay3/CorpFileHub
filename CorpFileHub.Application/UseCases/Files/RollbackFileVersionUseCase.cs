using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Files
{
    public class RollbackFileVersionUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditLogRepository _auditLogRepository;

        public RollbackFileVersionUseCase(
            IFileRepository fileRepository,
            IYandexDiskService yandexDiskService,
            IFileStorageService fileStorageService,
            IAuditLogRepository auditLogRepository)
        {
            _fileRepository = fileRepository;
            _yandexDiskService = yandexDiskService;
            _fileStorageService = fileStorageService;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<bool> ExecuteAsync(int fileId, int targetVersion, int userId, string comment = "")
        {
            // 1. Получаем файл
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.IsDeleted)
                throw new ArgumentException("Файл не найден");

            // 2. TODO: Проверяем права доступа

            // 3. Проверяем существование версии
            if (!await _fileStorageService.VersionExistsAsync(fileId, targetVersion))
                throw new ArgumentException("Версия файла не найдена");

            // 4. Получаем файл указанной версии
            using var versionStream = await _fileStorageService.GetFileVersionAsync(fileId, targetVersion);

            // 5. Загружаем версию обратно на Яндекс.Диск (заменяем текущий файл)
            await _yandexDiskService.UploadFileAsync(versionStream, file.Name, Path.GetDirectoryName(file.YandexDiskPath) ?? "");

            // 6. Создаем новую версию как "откат к версии X"
            versionStream.Position = 0;
            var newVersionNumber = file.Versions.Max(v => v.Version) + 1;
            var localPath = await _fileStorageService.SaveFileVersionAsync(versionStream, fileId, newVersionNumber, file.Name);

            var newVersion = new Domain.Entities.FileVersion
            {
                FileId = fileId,
                LocalPath = localPath,
                YandexDiskPath = file.YandexDiskPath,
                Version = newVersionNumber,
                Size = versionStream.Length,
                CreatedById = userId,
                Comment = $"Откат к версии {targetVersion}. {comment}".Trim()
            };

            file.Versions.Add(newVersion);

            // 7. Обновляем файл
            file.UpdatedAt = DateTime.UtcNow;
            await _fileRepository.UpdateAsync(file);

            // 8. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.VersionRollback,
                EntityType = "File",
                EntityId = fileId,
                EntityName = file.Name,
                Description = $"Файл '{file.Name}' откачен к версии {targetVersion}"
            });

            return true;
        }
    }
}