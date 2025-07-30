using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Application.Services;

namespace CorpFileHub.Application.UseCases.Files
{
    public class OpenForEditingUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IFileManagementService _fileManagementService;

        public OpenForEditingUseCase(
            IFileRepository fileRepository,
            IYandexDiskService yandexDiskService,
            IAuditLogRepository auditLogRepository,
            IFileManagementService fileManagementService)
        {
            _fileRepository = fileRepository;
            _yandexDiskService = yandexDiskService;
            _auditLogRepository = auditLogRepository;
            _fileManagementService = fileManagementService;
        }

        public async Task<string> ExecuteAsync(int fileId, int userId)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.IsDeleted)
                throw new ArgumentException("Файл не найден");

            var supportedExtensions = new[] { ".docx", ".xlsx", ".pptx" };
            if (!supportedExtensions.Contains(file.Extension.ToLower()))
                throw new InvalidOperationException("Формат файла не поддерживается для онлайн-редактирования");

            await _fileManagementService.MarkFileAsEditingAsync(fileId, userId);

            var editLink = await _yandexDiskService.GetEditLinkAsync(file.YandexDiskPath);

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
