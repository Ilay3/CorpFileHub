using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Folders
{
    public class CreateFolderUseCase
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public CreateFolderUseCase(
            IFolderRepository folderRepository,
            IAuditLogRepository auditLogRepository)
        {
            _folderRepository = folderRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<Folder> ExecuteAsync(string name, int? parentFolderId, int userId, string description = "")
        {
            // 1. Проверяем уникальность имени
            if (await _folderRepository.FolderExistsAsync(name, parentFolderId))
                throw new InvalidOperationException("Папка с таким именем уже существует");

            // 2. Получаем родительскую папку (если есть)
            Folder? parentFolder = null;
            if (parentFolderId.HasValue)
            {
                parentFolder = await _folderRepository.GetByIdAsync(parentFolderId.Value);
                if (parentFolder == null)
                    throw new ArgumentException("Родительская папка не найдена");
            }

            // 3. Формируем путь
            var path = parentFolder != null
                ? $"{parentFolder.Path}/{name}"
                : $"/{name}";

            var yandexPath = parentFolder != null
                ? $"{parentFolder.YandexDiskPath}/{name}"
                : name;

            // 4. Создаем папку
            var folder = new Folder
            {
                Name = name,
                Path = path,
                YandexDiskPath = yandexPath,
                ParentFolderId = parentFolderId,
                OwnerId = userId,
                Description = description
            };

            var createdFolder = await _folderRepository.CreateAsync(folder);

            // 5. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = userId,
                Action = AuditAction.FolderCreate,
                EntityType = "Folder",
                EntityId = createdFolder.Id,
                EntityName = name,
                Description = $"Папка '{name}' создана в '{parentFolder?.Name ?? "корне"}'"
            });

            return createdFolder;
        }
    }
}