using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Files
{
    public class DeleteFileUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IAuditLogRepository _auditLogRepository;

        public DeleteFileUseCase(
            IFileRepository fileRepository,
            IYandexDiskService yandexDiskService,
            IAuditLogRepository auditLogRepository)
        {
            _fileRepository = fileRepository;
            _yandexDiskService = yandexDiskService;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<bool> ExecuteAsync(int fileId, int userId)
        {
            // 1. Получаем файл
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.IsDeleted)
                return false;

            // 2. TODO: Проверяем права доступа

            // 3. Удаляем файл с Яндекс.Диска
            await _yandexDiskService.DeleteFileAsync(file.YandexDiskPath);

            // 4. Помечаем файл как удаленный (мягкое удаление)
            var result = await _fileRepository.DeleteAsync(fileId);

            // 5. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.FileDelete,
                EntityType = "File",
                EntityId = fileId,
                EntityName = file.Name,
                Description = $"Файл '{file.Name}' удален"
            });

            return result;
        }
    }
}