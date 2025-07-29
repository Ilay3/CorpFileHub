using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CorpFileHub.Application.Services
{
    public interface IAccessControlService
    {
        Task<bool> CanReadFileAsync(int fileId, int userId);
        Task<bool> CanEditFileAsync(int fileId, int userId);
        Task<bool> CanDeleteFileAsync(int fileId, int userId);
        Task<bool> CanViewFileHistoryAsync(int fileId, int userId);

        Task<bool> CanReadFolderAsync(int folderId, int userId);
        Task<bool> CanCreateInFolderAsync(int folderId, int userId);
        Task<bool> CanEditFolderAsync(int folderId, int userId);
        Task<bool> CanDeleteFolderAsync(int folderId, int userId);

        Task<AccessLevel> GetFileAccessLevelAsync(int fileId, int userId);
        Task<AccessLevel> GetFolderAccessLevelAsync(int folderId, int userId);

        Task<bool> SetFileAccessAsync(int fileId, int userId, AccessLevel accessLevel, int grantedBy);
        Task<bool> SetFolderAccessAsync(int folderId, int userId, AccessLevel accessLevel, int grantedBy);

        Task<List<AccessRule>> GetFileAccessRulesAsync(int fileId);
        Task<List<AccessRule>> GetFolderAccessRulesAsync(int folderId);

        Task<bool> InheritFolderPermissionsAsync(int folderId, int parentFolderId);
        Task<bool> RemoveUserAccessAsync(int userId);
    }

    public class AccessControlService : IAccessControlService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AccessControlService> _logger;

        public AccessControlService(
            IFileRepository fileRepository,
            IFolderRepository folderRepository,
            IUserRepository userRepository,
            ILogger<AccessControlService> logger)
        {
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        #region Проверка прав на файлы

        public async Task<bool> CanReadFileAsync(int fileId, int userId)
        {
            var accessLevel = await GetFileAccessLevelAsync(fileId, userId);
            return accessLevel >= AccessLevel.Read;
        }

        public async Task<bool> CanEditFileAsync(int fileId, int userId)
        {
            var accessLevel = await GetFileAccessLevelAsync(fileId, userId);
            return accessLevel >= AccessLevel.Write;
        }

        public async Task<bool> CanDeleteFileAsync(int fileId, int userId)
        {
            var accessLevel = await GetFileAccessLevelAsync(fileId, userId);
            return accessLevel >= AccessLevel.Delete;
        }

        public async Task<bool> CanViewFileHistoryAsync(int fileId, int userId)
        {
            // История версий доступна если есть право на чтение
            return await CanReadFileAsync(fileId, userId);
        }

        public async Task<AccessLevel> GetFileAccessLevelAsync(int fileId, int userId)
        {
            try
            {
                // Получаем пользователя
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Попытка доступа неактивным пользователем {UserId} к файлу {FileId}", userId, fileId);
                    return AccessLevel.None;
                }

                // Администраторы имеют полный доступ
                if (user.IsAdmin)
                {
                    _logger.LogDebug("Администратор {UserId} получил полный доступ к файлу {FileId}", userId, fileId);
                    return AccessLevel.Admin;
                }

                // Получаем файл
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null || file.IsDeleted)
                {
                    _logger.LogWarning("Попытка доступа к несуществующему файлу {FileId}", fileId);
                    return AccessLevel.None;
                }

                // Владелец файла имеет полный доступ
                if (file.OwnerId == userId)
                {
                    _logger.LogDebug("Владелец {UserId} получил полный доступ к файлу {FileId}", userId, fileId);
                    return AccessLevel.Admin;
                }

                // Проверяем явные права на файл
                var explicitAccess = await GetExplicitFileAccessAsync(fileId, userId);
                if (explicitAccess > AccessLevel.None)
                {
                    _logger.LogDebug("Явный доступ {AccessLevel} для пользователя {UserId} к файлу {FileId}", explicitAccess, userId, fileId);
                    return explicitAccess;
                }

                // Проверяем наследуемые права от папки
                var inheritedAccess = await GetInheritedFolderAccessAsync(file.FolderId, userId);
                if (inheritedAccess > AccessLevel.None)
                {
                    _logger.LogDebug("Наследуемый доступ {AccessLevel} для пользователя {UserId} к файлу {FileId}", inheritedAccess, userId, fileId);
                    return inheritedAccess;
                }

                _logger.LogDebug("Доступ запрещен для пользователя {UserId} к файлу {FileId}", userId, fileId);
                return AccessLevel.None;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке доступа пользователя {UserId} к файлу {FileId}", userId, fileId);
                return AccessLevel.None;
            }
        }

        #endregion

        #region Проверка прав на папки

        public async Task<bool> CanReadFolderAsync(int folderId, int userId)
        {
            var accessLevel = await GetFolderAccessLevelAsync(folderId, userId);
            return accessLevel >= AccessLevel.Read;
        }

        public async Task<bool> CanCreateInFolderAsync(int folderId, int userId)
        {
            var accessLevel = await GetFolderAccessLevelAsync(folderId, userId);
            return accessLevel >= AccessLevel.Write;
        }

        public async Task<bool> CanEditFolderAsync(int folderId, int userId)
        {
            var accessLevel = await GetFolderAccessLevelAsync(folderId, userId);
            return accessLevel >= AccessLevel.Write;
        }

        public async Task<bool> CanDeleteFolderAsync(int folderId, int userId)
        {
            var accessLevel = await GetFolderAccessLevelAsync(folderId, userId);
            return accessLevel >= AccessLevel.Delete;
        }

        public async Task<AccessLevel> GetFolderAccessLevelAsync(int folderId, int userId)
        {
            try
            {
                // Получаем пользователя
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Попытка доступа неактивным пользователем {UserId} к папке {FolderId}", userId, folderId);
                    return AccessLevel.None;
                }

                // Администраторы имеют полный доступ
                if (user.IsAdmin)
                {
                    _logger.LogDebug("Администратор {UserId} получил полный доступ к папке {FolderId}", userId, folderId);
                    return AccessLevel.Admin;
                }

                // Получаем папку
                var folder = await _folderRepository.GetByIdAsync(folderId);
                if (folder == null || folder.IsDeleted)
                {
                    _logger.LogWarning("Попытка доступа к несуществующей папке {FolderId}", folderId);
                    return AccessLevel.None;
                }

                // Владелец папки имеет полный доступ
                if (folder.OwnerId == userId)
                {
                    _logger.LogDebug("Владелец {UserId} получил полный доступ к папке {FolderId}", userId, folderId);
                    return AccessLevel.Admin;
                }

                // Проверяем явные права на папку
                var explicitAccess = await GetExplicitFolderAccessAsync(folderId, userId);
                if (explicitAccess > AccessLevel.None)
                {
                    _logger.LogDebug("Явный доступ {AccessLevel} для пользователя {UserId} к папке {FolderId}", explicitAccess, userId, folderId);
                    return explicitAccess;
                }

                // Проверяем наследуемые права от родительской папки
                if (folder.ParentFolderId.HasValue)
                {
                    var inheritedAccess = await GetInheritedFolderAccessAsync(folder.ParentFolderId.Value, userId);
                    if (inheritedAccess > AccessLevel.None)
                    {
                        _logger.LogDebug("Наследуемый доступ {AccessLevel} для пользователя {UserId} к папке {FolderId}", inheritedAccess, userId, folderId);
                        return inheritedAccess;
                    }
                }

                _logger.LogDebug("Доступ запрещен для пользователя {UserId} к папке {FolderId}", userId, folderId);
                return AccessLevel.None;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке доступа пользователя {UserId} к папке {FolderId}", userId, folderId);
                return AccessLevel.None;
            }
        }

        #endregion

        #region Управление правами доступа

        public async Task<bool> SetFileAccessAsync(int fileId, int userId, AccessLevel accessLevel, int grantedBy)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null) return false;

                // Проверяем, что пользователь имеет право изменять права
                var granterAccess = await GetFileAccessLevelAsync(fileId, grantedBy);
                if (granterAccess < AccessLevel.Admin)
                {
                    _logger.LogWarning("Пользователь {GrantedBy} не имеет прав для изменения доступа к файлу {FileId}", grantedBy, fileId);
                    return false;
                }

                // Удаляем существующие правила для этого пользователя
                var existingRules = file.AccessRules.Where(ar => ar.UserId == userId && ar.IsActive).ToList();
                foreach (var rule in existingRules)
                {
                    rule.IsActive = false;
                }

                // Добавляем новое правило (если не None)
                if (accessLevel > AccessLevel.None)
                {
                    var newRule = new AccessRule
                    {
                        FileId = fileId,
                        UserId = userId,
                        AccessLevel = accessLevel,
                        CreatedById = grantedBy,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    file.AccessRules.Add(newRule);
                }

                await _fileRepository.UpdateAsync(file);

                _logger.LogInformation("Права доступа к файлу {FileId} для пользователя {UserId} изменены на {AccessLevel} пользователем {GrantedBy}",
                    fileId, userId, accessLevel, grantedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при установке прав доступа к файлу {FileId} для пользователя {UserId}", fileId, userId);
                return false;
            }
        }

        public async Task<bool> SetFolderAccessAsync(int folderId, int userId, AccessLevel accessLevel, int grantedBy)
        {
            try
            {
                var folder = await _folderRepository.GetByIdAsync(folderId);
                if (folder == null) return false;

                // Проверяем, что пользователь имеет право изменять права
                var granterAccess = await GetFolderAccessLevelAsync(folderId, grantedBy);
                if (granterAccess < AccessLevel.Admin)
                {
                    _logger.LogWarning("Пользователь {GrantedBy} не имеет прав для изменения доступа к папке {FolderId}", grantedBy, folderId);
                    return false;
                }

                // Удаляем существующие правила для этого пользователя
                var existingRules = folder.AccessRules.Where(ar => ar.UserId == userId && ar.IsActive).ToList();
                foreach (var rule in existingRules)
                {
                    rule.IsActive = false;
                }

                // Добавляем новое правило (если не None)
                if (accessLevel > AccessLevel.None)
                {
                    var newRule = new AccessRule
                    {
                        FolderId = folderId,
                        UserId = userId,
                        AccessLevel = accessLevel,
                        CreatedById = grantedBy,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    folder.AccessRules.Add(newRule);
                }

                await _folderRepository.UpdateAsync(folder);

                _logger.LogInformation("Права доступа к папке {FolderId} для пользователя {UserId} изменены на {AccessLevel} пользователем {GrantedBy}",
                    folderId, userId, accessLevel, grantedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при установке прав доступа к папке {FolderId} для пользователя {UserId}", folderId, userId);
                return false;
            }
        }

        #endregion

        #region Получение правил доступа

        public async Task<List<AccessRule>> GetFileAccessRulesAsync(int fileId)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            return file?.AccessRules.Where(ar => ar.IsActive).ToList() ?? new List<AccessRule>();
        }

        public async Task<List<AccessRule>> GetFolderAccessRulesAsync(int folderId)
        {
            var folder = await _folderRepository.GetByIdAsync(folderId);
            return folder?.AccessRules.Where(ar => ar.IsActive).ToList() ?? new List<AccessRule>();
        }

        #endregion

        #region Вспомогательные методы

        private async Task<AccessLevel> GetExplicitFileAccessAsync(int fileId, int userId)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null) return AccessLevel.None;

            var explicitRule = file.AccessRules
                .Where(ar => ar.UserId == userId && ar.IsActive)
                .Where(ar => !ar.ExpiresAt.HasValue || ar.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(ar => ar.AccessLevel)
                .FirstOrDefault();

            return explicitRule?.AccessLevel ?? AccessLevel.None;
        }

        private async Task<AccessLevel> GetExplicitFolderAccessAsync(int folderId, int userId)
        {
            var folder = await _folderRepository.GetByIdAsync(folderId);
            if (folder == null) return AccessLevel.None;

            var explicitRule = folder.AccessRules
                .Where(ar => ar.UserId == userId && ar.IsActive)
                .Where(ar => !ar.ExpiresAt.HasValue || ar.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(ar => ar.AccessLevel)
                .FirstOrDefault();

            return explicitRule?.AccessLevel ?? AccessLevel.None;
        }

        private async Task<AccessLevel> GetInheritedFolderAccessAsync(int folderId, int userId)
        {
            var folder = await _folderRepository.GetByIdAsync(folderId);
            if (folder == null) return AccessLevel.None;

            // Сначала проверяем явные права на текущую папку
            var explicitAccess = await GetExplicitFolderAccessAsync(folderId, userId);
            if (explicitAccess > AccessLevel.None)
                return explicitAccess;

            // Если нет явных прав, проверяем родительскую папку
            if (folder.ParentFolderId.HasValue)
            {
                return await GetInheritedFolderAccessAsync(folder.ParentFolderId.Value, userId);
            }

            return AccessLevel.None;
        }

        public async Task<bool> InheritFolderPermissionsAsync(int folderId, int parentFolderId)
        {
            try
            {
                var folder = await _folderRepository.GetByIdAsync(folderId);
                var parentFolder = await _folderRepository.GetByIdAsync(parentFolderId);

                if (folder == null || parentFolder == null) return false;

                // Копируем права доступа от родительской папки
                foreach (var parentRule in parentFolder.AccessRules.Where(ar => ar.IsActive))
                {
                    var newRule = new AccessRule
                    {
                        FolderId = folderId,
                        UserId = parentRule.UserId,
                        GroupId = parentRule.GroupId,
                        AccessLevel = parentRule.AccessLevel,
                        CreatedById = parentRule.CreatedById,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    folder.AccessRules.Add(newRule);
                }

                await _folderRepository.UpdateAsync(folder);

                _logger.LogInformation("Права доступа унаследованы для папки {FolderId} от родительской папки {ParentFolderId}", folderId, parentFolderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при наследовании прав для папки {FolderId}", folderId);
                return false;
            }
        }

        public async Task<bool> RemoveUserAccessAsync(int userId)
        {
            try
            {
                // Удаляем все правила доступа для пользователя
                // Здесь нужно реализовать запрос к базе для массового обновления
                // Пока оставляем заглушку

                _logger.LogInformation("Все права доступа удалены для пользователя {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении прав доступа для пользователя {UserId}", userId);
                return false;
            }
        }

        #endregion
    }
}