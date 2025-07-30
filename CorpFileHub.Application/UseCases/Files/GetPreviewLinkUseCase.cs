using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;

namespace CorpFileHub.Application.UseCases.Files
{
    public class GetPreviewLinkUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IAuditLogRepository _auditLogRepository;

        public GetPreviewLinkUseCase(
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
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.IsDeleted)
                throw new ArgumentException("Файл не найден");

            // TODO: проверка прав доступа

            var link = await _yandexDiskService.GetDownloadLinkAsync(file.YandexDiskPath);

            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.FileView,
                EntityType = "File",
                EntityId = fileId,
                EntityName = file.Name,
                Description = $"Предпросмотр файла '{file.Name}'"
            });

            return link;
        }
    }
}
