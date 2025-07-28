using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Files
{
    public class OpenForEditingUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IAuditLogRepository _auditLogRepository;

        public OpenForEditingUseCase(
            IFileRepository fileRepository,
            IYandexDiskService yandexDiskService,
            IAuditLogRepository auditLogRepository)
        {
            _fileRepository = fileRepository;
            _yandexDiskService = yandexDiskService;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<string> ExecuteAsync(int fileId, int userId)
        {
            // 1. Получаем файл
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.IsDeleted)
                throw new ArgumentException("Файл не найден");

            // 2. Проверяем поддерживаемые форматы
            var supportedExtensions = new[] { ".docx", ".xlsx", ".pptx" };
            if (!supportedExtensions.Contains(file.Extension.ToLower()))
                throw new InvalidOperationException("Формат файла не поддерживается для онлайн-редактирования");

            // 3. TODO: Проверяем права доступа

            // 4. Обновляем статус файла
            file.Status = FileStatus.InEditing;
            await _fileRepository.UpdateAsync(file);

            // 5. Получаем ссылку для редактирования
            var editLink = await _yandexDiskService.GetEditLinkAsync(file.YandexDiskPath);

            // 6. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.FileEdit,
                EntityType = "File",
                EntityId = fileId,
                EntityName = file.Name,
                Description = $"Файл '{file.Name}' открыт для редактирования"
            });

            return editLink;
        }
    }
}