using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace CorpFileHub.Application.Services
{
    public interface IFileManagementService
    {
        Task<FileItem?> GetFileWithAccessCheckAsync(int fileId, int userId);
        Task<List<FileItem>> GetUserAccessibleFilesAsync(int userId, int? folderId = null);
        Task<FileVersion?> CreateVersionAsync(int fileId, int userId, string comment = "");
        Task<List<FileVersion>> GetFileVersionsAsync(int fileId, int userId);
        Task<bool> RestoreVersionAsync(int fileId, int versionId, int userId);
        Task<string> GetFileHashAsync(Stream fileStream);
        Task<bool> CheckFileIntegrityAsync(int fileId);
        Task<FileStats> GetFileStatsAsync(int fileId);
        Task<UserFileStats> GetUserFileStatsAsync(int userId);
        Task<bool> MoveFileToFolderAsync(int fileId, int targetFolderId, int userId);
        Task<bool> RenameFileAsync(int fileId, string newName, int userId);
        Task<List<FileItem>> SearchUserFilesAsync(int userId, string query, SearchFilters? filters = null);
        Task<List<FileItem>> SearchUserFilesAdvancedAsync(int userId, SearchFilters filters);
        Task<bool> SetFileTagsAsync(int fileId, string tags, int userId);
        Task<bool> MarkFileAsEditingAsync(int fileId, int userId);
        Task<bool> UnmarkFileAsEditingAsync(int fileId, int userId);
        Task<List<FileItem>> GetRecentFilesAsync(int userId, int count = 10);
        Task<List<FileItem>> GetFavoriteFilesAsync(int userId);
        Task<bool> ToggleFavoriteAsync(int fileId, int userId);
        Task<(Stream fileStream, string fileName, string contentType)?> GetFileVersionStreamAsync(int fileId, int versionId, int userId);

        /// <summary>
        /// Очистить старые версии файлов согласно политике хранения
        /// </summary>
        Task<int> CleanupOldVersionsAsync();
    }

    public class FileManagementService : IFileManagementService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IAuditService _auditService;
        private readonly ILogger<FileManagementService> _logger;
        private readonly IConfiguration _configuration;

        public FileManagementService(
            IFileRepository fileRepository,
            IAccessControlService accessControlService,
            IFileStorageService fileStorageService,
            IYandexDiskService yandexDiskService,
            IAuditService auditService,
            ILogger<FileManagementService> logger,
            IConfiguration configuration)
        {
            _fileRepository = fileRepository;
            _accessControlService = accessControlService;
            _fileStorageService = fileStorageService;
            _yandexDiskService = yandexDiskService;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<FileItem?> GetFileWithAccessCheckAsync(int fileId, int userId)
        {
            try
            {
                var hasAccess = await _accessControlService.CanReadFileAsync(fileId, userId);
                if (!hasAccess)
                {
                    await _auditService.LogErrorAsync(userId, AuditAction.FileView, "File", fileId, 
                        "", "Попытка доступа к файлу без прав", "Недостаточно прав");
                    return null;
                }

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file != null)
                {
                    await _auditService.LogSuccessAsync(userId, AuditAction.FileView, "File", fileId, 
                        file.Name, "Просмотр файла");
                }

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения файла {FileId} для пользователя {UserId}", fileId, userId);
                return null;
            }
        }

        public async Task<List<FileItem>> GetUserAccessibleFilesAsync(int userId, int? folderId = null)
        {
            try
            {
                var allFiles = folderId.HasValue 
                    ? await _fileRepository.GetByFolderIdAsync(folderId.Value)
                    : await _fileRepository.GetByOwnerIdAsync(userId);

                var accessibleFiles = new List<FileItem>();

                foreach (var file in allFiles)
                {
                    if (await _accessControlService.CanReadFileAsync(file.Id, userId))
                    {
                        accessibleFiles.Add(file);
                    }
                }

                return accessibleFiles.OrderByDescending(f => f.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения доступных файлов для пользователя {UserId}", userId);
                return new List<FileItem>();
            }
        }

        public async Task<FileVersion?> CreateVersionAsync(int fileId, int userId, string comment = "")
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return null;

                var canEdit = await _accessControlService.CanEditFileAsync(fileId, userId);
                if (!canEdit)
                {
                    await _auditService.LogErrorAsync(userId, AuditAction.VersionCreate, "FileVersion", fileId,
                        file.Name, "Попытка создания версии без прав", "Недостаточно прав");
                    return null;
                }

                // Скачиваем актуальную версию с Яндекс.Диска
                using var fileStream = await _yandexDiskService.DownloadFileAsync(file.YandexDiskPath);
                
                var newVersionNumber = file.Versions.Any() ? file.Versions.Max(v => v.Version) + 1 : 1;
                var hash = await GetFileHashAsync(fileStream);
                
                // Сохраняем версию локально
                fileStream.Position = 0;
                var localPath = await _fileStorageService.SaveFileVersionAsync(fileStream, fileId, newVersionNumber, file.Name);

                // Деактивируем ранее активные версии
                foreach (var v in file.Versions.Where(v => v.IsActive))
                {
                    v.IsActive = false;
                }

                var fileVersion = new FileVersion
                {
                    FileId = fileId,
                    LocalPath = localPath,
                    YandexDiskPath = file.YandexDiskPath,
                    Version = newVersionNumber,
                    Size = fileStream.Length,
                    CreatedById = userId,
                    Comment = comment,
                    Hash = hash,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Добавляем версию к файлу
                file.Versions.Add(fileVersion);
                file.UpdatedAt = DateTime.UtcNow;
                await _fileRepository.UpdateAsync(file);

                await _auditService.LogSuccessAsync(userId, AuditAction.VersionCreate, "FileVersion", fileId,
                    file.Name, $"Создана версия {newVersionNumber} файла '{file.Name}'");

                return fileVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания версии файла {FileId}", fileId);
                await _auditService.LogErrorAsync(userId, AuditAction.VersionCreate, "FileVersion", fileId,
                    "", "Ошибка создания версии", ex.Message);
                return null;
            }
        }

        public async Task<List<FileVersion>> GetFileVersionsAsync(int fileId, int userId)
        {
            try
            {
                var canViewHistory = await _accessControlService.CanViewFileHistoryAsync(fileId, userId);
                if (!canViewHistory) return new List<FileVersion>();

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return new List<FileVersion>();

                await _auditService.LogSuccessAsync(userId, AuditAction.AccessRightsView, "FileVersion", fileId,
                    file.Name, "Просмотр истории версий");

                return file.Versions.OrderByDescending(v => v.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения версий файла {FileId}", fileId);
                return new List<FileVersion>();
            }
        }

        public async Task<bool> RestoreVersionAsync(int fileId, int versionId, int userId)
        {
            try
            {
                var canEdit = await _accessControlService.CanEditFileAsync(fileId, userId);
                if (!canEdit) return false;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                var targetVersion = file.Versions.FirstOrDefault(v => v.Id == versionId);
                if (targetVersion == null) return false;

                // Создаем резервную копию текущей версии
                await CreateVersionAsync(fileId, userId, "Автоматическая версия перед откатом");

                // Восстанавливаем версию
                using var versionStream = await _fileStorageService.GetFileVersionAsync(fileId, targetVersion.Version);
                await _yandexDiskService.UploadFileAsync(versionStream, file.Name, Path.GetDirectoryName(file.YandexDiskPath) ?? "");

                file.UpdatedAt = DateTime.UtcNow;
                await _fileRepository.UpdateAsync(file);

                await _auditService.LogSuccessAsync(userId, AuditAction.VersionRollback, "File", fileId,
                    file.Name, $"Откат к версии {targetVersion.Version}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка восстановления версии {VersionId} файла {FileId}", versionId, fileId);
                return false;
            }
        }

        public async Task<string> GetFileHashAsync(Stream fileStream)
        {
            try
            {
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(fileStream);
                return Convert.ToHexString(hashBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка вычисления хэша файла");
                return "";
            }
        }

        public async Task<bool> CheckFileIntegrityAsync(int fileId)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                // Проверяем последнюю версию
                var lastVersion = file.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                if (lastVersion == null) return true;

                // Сравниваем хэши
                using var localVersionStream = await _fileStorageService.GetFileVersionAsync(fileId, lastVersion.Version);
                var currentHash = await GetFileHashAsync(localVersionStream);

                var isIntact = currentHash.Equals(lastVersion.Hash, StringComparison.OrdinalIgnoreCase);
                
                if (!isIntact)
                {
                    await _auditService.LogSystemActionAsync(AuditAction.SystemError, 
                        $"Обнаружено нарушение целостности файла {file.Name} (ID: {fileId})");
                }

                return isIntact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки целостности файла {FileId}", fileId);
                return false;
            }
        }

        public async Task<FileStats> GetFileStatsAsync(int fileId)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return new FileStats();

                return new FileStats
                {
                    FileId = fileId,
                    TotalVersions = file.Versions.Count,
                    TotalSize = file.Versions.Sum(v => v.Size),
                    CreatedAt = file.CreatedAt,
                    LastModified = file.UpdatedAt,
                    LastVersion = file.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault()?.Version ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статистики файла {FileId}", fileId);
                return new FileStats();
            }
        }

        public async Task<UserFileStats> GetUserFileStatsAsync(int userId)
        {
            try
            {
                var userFiles = await _fileRepository.GetByOwnerIdAsync(userId);
                
                return new UserFileStats
                {
                    UserId = userId,
                    TotalFiles = userFiles.Count(),
                    TotalSize = userFiles.Sum(f => f.Size),
                    TotalVersions = userFiles.SelectMany(f => f.Versions).Count(),
                    LastActivity = userFiles.Max(f => f.UpdatedAt)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статистики пользователя {UserId}", userId);
                return new UserFileStats { UserId = userId };
            }
        }

        public async Task<bool> MoveFileToFolderAsync(int fileId, int targetFolderId, int userId)
        {
            try
            {
                var canEdit = await _accessControlService.CanEditFileAsync(fileId, userId);
                var canCreateInTarget = await _accessControlService.CanCreateInFolderAsync(targetFolderId, userId);
                
                if (!canEdit || !canCreateInTarget) return false;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                var oldFolderId = file.FolderId;
                file.FolderId = targetFolderId;
                file.UpdatedAt = DateTime.UtcNow;

                await _fileRepository.UpdateAsync(file);

                await _auditService.LogSuccessAsync(userId, AuditAction.FileMove, "File", fileId,
                    file.Name, $"Файл перемещен из папки {oldFolderId} в папку {targetFolderId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перемещения файла {FileId}", fileId);
                return false;
            }
        }

        public async Task<bool> RenameFileAsync(int fileId, string newName, int userId)
        {
            try
            {
                var canEdit = await _accessControlService.CanEditFileAsync(fileId, userId);
                if (!canEdit) return false;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                var oldName = file.Name;
                file.Name = newName;
                file.UpdatedAt = DateTime.UtcNow;

                await _fileRepository.UpdateAsync(file);

                await _auditService.LogSuccessAsync(userId, AuditAction.FileRename, "File", fileId,
                    newName, $"Файл переименован с '{oldName}' на '{newName}'");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка переименования файла {FileId}", fileId);
                return false;
            }
        }

        public async Task<List<FileItem>> SearchUserFilesAsync(int userId, string query, SearchFilters? filters = null)
        {
            try
            {
                var searchResults = await _fileRepository.SearchAsync(query, filters?.FolderId);
                var accessibleFiles = new List<FileItem>();

                foreach (var file in searchResults)
                {
                    if (await _accessControlService.CanReadFileAsync(file.Id, userId))
                    {
                        accessibleFiles.Add(file);
                    }
                }

                await _auditService.LogSuccessAsync(userId, AuditAction.Search, "File", null,
                    "", $"Поиск файлов по запросу '{query}'");

                return accessibleFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска файлов для пользователя {UserId}", userId);
                return new List<FileItem>();
            }
        }

        public async Task<List<FileItem>> SearchUserFilesAdvancedAsync(int userId, SearchFilters filters)
        {
            try
            {
                var searchResults = await _fileRepository.SearchAdvancedAsync(
                    filters.Query,
                    filters.FolderId,
                    filters.DateFrom,
                    filters.DateTo,
                    filters.Extension,
                    filters.OwnerId,
                    filters.Tags,
                    filters.MinSize,
                    filters.MaxSize);

                var accessibleFiles = new List<FileItem>();
                foreach (var file in searchResults)
                {
                    if (await _accessControlService.CanReadFileAsync(file.Id, userId))
                    {
                        accessibleFiles.Add(file);
                    }
                }

                await _auditService.LogSuccessAsync(userId, AuditAction.Search, "File", null,
                    "", $"Расширенный поиск файлов по запросу '{filters.Query}'");

                return accessibleFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка расширенного поиска файлов для пользователя {UserId}", userId);
                return new List<FileItem>();
            }
        }

        public async Task<bool> SetFileTagsAsync(int fileId, string tags, int userId)
        {
            try
            {
                var canEdit = await _accessControlService.CanEditFileAsync(fileId, userId);
                if (!canEdit) return false;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                file.Tags = tags;
                file.UpdatedAt = DateTime.UtcNow;
                await _fileRepository.UpdateAsync(file);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка установки тегов для файла {FileId}", fileId);
                return false;
            }
        }

        public async Task<bool> MarkFileAsEditingAsync(int fileId, int userId)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                file.Status = FileStatus.InEditing;
                file.UpdatedAt = DateTime.UtcNow;
                await _fileRepository.UpdateAsync(file);

                await _auditService.LogSuccessAsync(userId, AuditAction.FileEdit, "File", fileId,
                    file.Name, "Файл отмечен как редактируемый");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки файла {FileId} как редактируемого", fileId);
                return false;
            }
        }

        public async Task<bool> UnmarkFileAsEditingAsync(int fileId, int userId)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                file.Status = FileStatus.Active;
                file.UpdatedAt = DateTime.UtcNow;
                await _fileRepository.UpdateAsync(file);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка снятия отметки редактирования с файла {FileId}", fileId);
                return false;
            }
        }

        public async Task<List<FileItem>> GetRecentFilesAsync(int userId, int count = 10)
        {
            try
            {
                var userFiles = await _fileRepository.GetByOwnerIdAsync(userId);
                return userFiles.OrderByDescending(f => f.UpdatedAt).Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения недавних файлов для пользователя {UserId}", userId);
                return new List<FileItem>();
            }
        }

        public async Task<List<FileItem>> GetFavoriteFilesAsync(int userId)
        {
            try
            {
                // TODO: Реализовать систему избранных файлов
                return new List<FileItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения избранных файлов для пользователя {UserId}", userId);
                return new List<FileItem>();
            }
        }

        public async Task<bool> ToggleFavoriteAsync(int fileId, int userId)
        {
            try
            {
                // TODO: Реализовать систему избранных файлов
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка переключения избранного для файла {FileId}", fileId);
                return false;
            }
        }

        public async Task<(Stream fileStream, string fileName, string contentType)?> GetFileVersionStreamAsync(int fileId, int versionId, int userId)
        {
            try
            {
                var canRead = await _accessControlService.CanReadFileAsync(fileId, userId);
                if (!canRead)
                    return null;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                    return null;

                var version = file.Versions.FirstOrDefault(v => v.Id == versionId);
                if (version == null)
                    return null;

                var stream = await _fileStorageService.GetFileVersionAsync(fileId, version.Version);
                return (stream, file.Name, file.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения версии файла {FileId}", fileId);
                return null;
            }
        }

        public async Task<int> CleanupOldVersionsAsync()
        {
            var retentionDays = _configuration.GetValue<int>("Versioning:RetentionDays", 365);
            var maxVersions = _configuration.GetValue<int>("Versioning:MaxVersionsPerFile", 10);

            try
            {
                var files = await _fileRepository.GetAllWithVersionsAsync();
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                int removed = 0;

                foreach (var file in files)
                {
                    var ordered = file.Versions.OrderByDescending(v => v.CreatedAt).ToList();
                    var keep = ordered.Take(maxVersions).ToList();
                    keep.AddRange(ordered.Where(v => v.CreatedAt >= cutoffDate && !keep.Contains(v)));
                    var toDelete = ordered.Except(keep).ToList();

                    foreach (var version in toDelete)
                    {
                        await _fileStorageService.DeleteFileVersionAsync(file.Id, version.Version);
                        await _fileRepository.DeleteFileVersionAsync(version.Id);
                        removed++;
                    }
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка очистки старых версий файлов");
                return 0;
            }
        }
    }

    // Вспомогательные классы
    public class FileStats
    {
        public int FileId { get; set; }
        public int TotalVersions { get; set; }
        public long TotalSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public int LastVersion { get; set; }
    }

    public class UserFileStats
    {
        public int UserId { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int TotalVersions { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class SearchFilters
    {
        public string? Query { get; set; }
        public int? FolderId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Extension { get; set; }
        public string? Tags { get; set; }
        public int? OwnerId { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
    }
}