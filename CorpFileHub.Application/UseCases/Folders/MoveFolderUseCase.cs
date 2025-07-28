using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Folders
{
    public class MoveFolderUseCase
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public MoveFolderUseCase(
            IFolderRepository folderRepository,
            IAuditLogRepository auditLogRepository)
        {
            _folderRepository = folderRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<bool> ExecuteAsync(int folderId, int? newParentFolderId, int userId)
        {
            // 1. Получаем папку для перемещения
            var folder = await _folderRepository.GetByIdAsync(folderId);
            if (folder == null || folder.IsDeleted)
                throw new ArgumentException("Папка не найдена");

            // 2. Получаем новую родительскую папку
            Domain.Entities.Folder? newParentFolder = null;
            if (newParentFolderId.HasValue)
            {
                newParentFolder = await _folderRepository.GetByIdAsync(newParentFolderId.Value);
                if (newParentFolder == null)
                    throw new ArgumentException("Целевая папка не найдена");

                // Проверяем, что не перемещаем папку в саму себя или в дочернюю папку
                if (IsCircularMove(folder, newParentFolder))
                    throw new InvalidOperationException("Нельзя переместить папку в саму себя или в дочернюю папку");
            }

            // 3. Проверяем уникальность имени в новой папке
            if (await _folderRepository.FolderExistsAsync(folder.Name, newParentFolderId))
                throw new InvalidOperationException("Папка с таким именем уже существует в целевой папке");

            // 4. Обновляем пути
            var oldPath = folder.Path;
            var newPath = newParentFolder != null
                ? $"{newParentFolder.Path}/{folder.Name}"
                : $"/{folder.Name}";

            folder.ParentFolderId = newParentFolderId;
            folder.Path = newPath;
            folder.YandexDiskPath = newParentFolder != null
                ? $"{newParentFolder.YandexDiskPath}/{folder.Name}"
                : folder.Name;

            // 5. Обновляем пути всех дочерних элементов (рекурсивно)
            await UpdateChildrenPaths(folder, oldPath, newPath);

            // 6. Сохраняем изменения
            await _folderRepository.UpdateAsync(folder);

            // 7. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = AuditAction.FolderMove,
                EntityType = "Folder",
                EntityId = folderId,
                EntityName = folder.Name,
                Description = $"Папка '{folder.Name}' перемещена из '{Path.GetDirectoryName(oldPath)}' в '{newParentFolder?.Name ?? "корень"}'"
            });

            return true;
        }

        private bool IsCircularMove(Domain.Entities.Folder sourceFolder, Domain.Entities.Folder targetFolder)
        {
            var current = targetFolder;
            while (current != null)
            {
                if (current.Id == sourceFolder.Id)
                    return true;
                current = current.ParentFolder;
            }
            return false;
        }

        private async Task UpdateChildrenPaths(Domain.Entities.Folder folder, string oldBasePath, string newBasePath)
        {
            // Обновляем пути дочерних папок
            foreach (var subFolder in folder.SubFolders.Where(sf => !sf.IsDeleted))
            {
                var oldSubPath = subFolder.Path;
                subFolder.Path = subFolder.Path.Replace(oldBasePath, newBasePath);
                subFolder.YandexDiskPath = subFolder.YandexDiskPath.Replace(oldBasePath, newBasePath);

                await _folderRepository.UpdateAsync(subFolder);
                await UpdateChildrenPaths(subFolder, oldSubPath, subFolder.Path);
            }

            // Обновляем пути файлов
            foreach (var file in folder.Files.Where(f => !f.IsDeleted))
            {
                file.Path = file.Path.Replace(oldBasePath, newBasePath);
                file.YandexDiskPath = file.YandexDiskPath.Replace(oldBasePath, newBasePath);
                // Файлы обновим через FileRepository при необходимости
            }
        }
    }
}