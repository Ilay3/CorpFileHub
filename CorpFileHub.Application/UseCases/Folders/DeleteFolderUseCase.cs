using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Folders
{
    public class DeleteFolderUseCase
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public DeleteFolderUseCase(
            IFolderRepository folderRepository,
            IFileRepository fileRepository,
            IAuditLogRepository auditLogRepository)
        {
            _folderRepository = folderRepository;
            _fileRepository = fileRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<bool> ExecuteAsync(int folderId, int userId, bool force = false)
        {
            // 1. Получаем папку
            var folder = await _folderRepository.GetByIdAsync(folderId);
            if (folder == null || folder.IsDeleted)
                return false;

            // 2. TODO: Проверяем права доступа

            // 3. Проверяем наличие файлов и подпапок
            if (!force)
            {
                var hasFiles = folder.Files.Any(f => !f.IsDeleted);
                var hasSubfolders = folder.SubFolders.Any(sf => !sf.IsDeleted);

                if (hasFiles || hasSubfolders)
                    throw new InvalidOperationException("Папка не пуста. Используйте принудительное удаление или очистите папку");
            }

            // 4. Удаляем папку (мягкое удаление)
            var result = await _folderRepository.DeleteAsync(folderId);

            // 5. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.FolderDelete,
                EntityType = "Folder",
                EntityId = folderId,
                EntityName = folder.Name,
                Description = $"Папка '{folder.Name}' удалена"
            });

            return result;
        }
    }
}