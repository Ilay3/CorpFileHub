using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;

namespace CorpFileHub.Application.UseCases.Access
{
    public class SetAccessRightsUseCase
    {
        private readonly IAccessControlService _accessControlService;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly INotificationService _notificationService;

        public SetAccessRightsUseCase(
            IAccessControlService accessControlService,
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            IFileRepository fileRepository,
            IFolderRepository folderRepository,
            INotificationService notificationService)
        {
            _accessControlService = accessControlService;
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _notificationService = notificationService;
        }

        public async Task<AccessRightsResult> SetFileAccessAsync(SetFileAccessRequest request)
        {
            var result = new AccessRightsResult
            {
                EntityId = request.FileId,
                EntityType = "File",
                GrantedBy = request.GrantedBy,
                Success = false
            };

            try
            {
                // Проверяем права текущего пользователя на изменение доступа
                var canChangeAccess = await _accessControlService.GetFileAccessLevelAsync(request.FileId, request.GrantedBy);
                if (canChangeAccess < AccessLevel.Admin)
                {
                    result.ErrorMessage = "Недостаточно прав для изменения доступа к файлу";

                    await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                    {
                        UserId = request.GrantedBy,
                        Action = AuditAction.AccessRightsChange,
                        EntityType = "File",
                        EntityId = request.FileId,
                        Description = "Отказ в изменении прав доступа - недостаточно полномочий",
                        IsSuccess = false,
                        ErrorMessage = result.ErrorMessage
                    });

                    return result;
                }

                // Получаем файл и пользователя
                var file = await _fileRepository.GetByIdAsync(request.FileId);
                var targetUser = await _userRepository.GetByIdAsync(request.UserId);

                if (file == null)
                {
                    result.ErrorMessage = "Файл не найден";
                    return result;
                }

                if (targetUser == null)
                {
                    result.ErrorMessage = "Пользователь не найден";
                    return result;
                }

                // Устанавливаем права доступа
                var success = await _accessControlService.SetFileAccessAsync(
                    request.FileId,
                    request.UserId,
                    request.AccessLevel,
                    request.GrantedBy);

                if (success)
                {
                    result.Success = true;
                    result.NewAccessLevel = request.AccessLevel;

                    // Создаем аудит лог
                    await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                    {
                        UserId = request.GrantedBy,
                        Action = AuditAction.AccessRightsChange,
                        EntityType = "File",
                        EntityId = request.FileId,
                        EntityName = file.Name,
                        Description = $"Изменены права доступа к файлу '{file.Name}' для пользователя '{targetUser.FullName}' на уровень '{GetAccessLevelText(request.AccessLevel)}'",
                        IsSuccess = true
                    });

                    // Отправляем уведомление пользователю о изменении прав
                    try
                    {
                        await _notificationService.SendAccessChangedNotificationAsync(
                            targetUser.Email,
                            file.Name,
                            GetAccessLevelText(request.AccessLevel));
                    }
                    catch (Exception ex)
                    {
                        // Ошибка отправки уведомления не должна прерывать основной процесс
                        await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                        {
                            UserId = request.GrantedBy,
                            Action = AuditAction.SystemError,
                            Description = "Ошибка отправки уведомления об изменении прав доступа",
                            ErrorMessage = ex.Message,
                            IsSuccess = false
                        });
                    }
                }
                else
                {
                    result.ErrorMessage = "Ошибка при установке прав доступа";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;

                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = request.GrantedBy,
                    Action = AuditAction.AccessRightsChange,
                    EntityType = "File",
                    EntityId = request.FileId,
                    Description = "Ошибка при изменении прав доступа к файлу",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });

                return result;
            }
        }

        public async Task<AccessRightsResult> SetFolderAccessAsync(SetFolderAccessRequest request)
        {
            var result = new AccessRightsResult
            {
                EntityId = request.FolderId,
                EntityType = "Folder",
                GrantedBy = request.GrantedBy,
                Success = false
            };

            try
            {
                // Проверяем права текущего пользователя на изменение доступа
                var canChangeAccess = await _accessControlService.GetFolderAccessLevelAsync(request.FolderId, request.GrantedBy);
                if (canChangeAccess < AccessLevel.Admin)
                {
                    result.ErrorMessage = "Недостаточно прав для изменения доступа к папке";

                    await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                    {
                        UserId = request.GrantedBy,
                        Action = AuditAction.AccessRightsChange,
                        EntityType = "Folder",
                        EntityId = request.FolderId,
                        Description = "Отказ в изменении прав доступа - недостаточно полномочий",
                        IsSuccess = false,
                        ErrorMessage = result.ErrorMessage
                    });

                    return result;
                }

                // Получаем папку и пользователя
                var folder = await _folderRepository.GetByIdAsync(request.FolderId);
                var targetUser = await _userRepository.GetByIdAsync(request.UserId);

                if (folder == null)
                {
                    result.ErrorMessage = "Папка не найдена";
                    return result;
                }

                if (targetUser == null)
                {
                    result.ErrorMessage = "Пользователь не найден";
                    return result;
                }

                // Устанавливаем права доступа
                var success = await _accessControlService.SetFolderAccessAsync(
                    request.FolderId,
                    request.UserId,
                    request.AccessLevel,
                    request.GrantedBy);

                if (success)
                {
                    result.Success = true;
                    result.NewAccessLevel = request.AccessLevel;

                    // Применяем права к дочерним элементам если указано
                    if (request.ApplyToChildren)
                    {
                        await ApplyRightsToChildrenAsync(request.FolderId, request.UserId, request.AccessLevel, request.GrantedBy);
                    }

                    // Создаем аудит лог
                    await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                    {
                        UserId = request.GrantedBy,
                        Action = AuditAction.AccessRightsChange,
                        EntityType = "Folder",
                        EntityId = request.FolderId,
                        EntityName = folder.Name,
                        Description = $"Изменены права доступа к папке '{folder.Name}' для пользователя '{targetUser.FullName}' на уровень '{GetAccessLevelText(request.AccessLevel)}'",
                        IsSuccess = true
                    });

                    // Отправляем уведомление пользователю о изменении прав
                    try
                    {
                        await _notificationService.SendAccessChangedNotificationAsync(
                            targetUser.Email,
                            folder.Name,
                            GetAccessLevelText(request.AccessLevel));
                    }
                    catch
                    {
                        // Игнорируем ошибки уведомлений
                    }
                }
                else
                {
                    result.ErrorMessage = "Ошибка при установке прав доступа";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;

                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = request.GrantedBy,
                    Action = AuditAction.AccessRightsChange,
                    EntityType = "Folder",
                    EntityId = request.FolderId,
                    Description = "Ошибка при изменении прав доступа к папке",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });

                return result;
            }
        }

        public async Task<List<UserAccessInfo>> GetFileAccessListAsync(int fileId, int requesterId)
        {
            // Проверяем права на просмотр списка доступа
            var requesterAccess = await _accessControlService.GetFileAccessLevelAsync(fileId, requesterId);
            if (requesterAccess < AccessLevel.Admin)
            {
                throw new UnauthorizedAccessException("Недостаточно прав для просмотра списка доступа");
            }

            var accessRules = await _accessControlService.GetFileAccessRulesAsync(fileId);
            var result = new List<UserAccessInfo>();

            foreach (var rule in accessRules)
            {
                if (rule.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(rule.UserId.Value);
                    if (user != null)
                    {
                        result.Add(new UserAccessInfo
                        {
                            UserId = user.Id,
                            UserName = user.FullName,
                            UserEmail = user.Email,
                            AccessLevel = rule.AccessLevel,
                            GrantedAt = rule.CreatedAt,
                            ExpiresAt = rule.ExpiresAt,
                            IsActive = rule.IsActive
                        });
                    }
                }
            }

            return result;
        }

        public async Task<List<UserAccessInfo>> GetFolderAccessListAsync(int folderId, int requesterId)
        {
            var requesterAccess = await _accessControlService.GetFolderAccessLevelAsync(folderId, requesterId);
            if (requesterAccess < AccessLevel.Admin)
                throw new UnauthorizedAccessException("Недостаточно прав для просмотра списка доступа");

            var accessRules = await _accessControlService.GetFolderAccessRulesAsync(folderId);
            var result = new List<UserAccessInfo>();

            foreach (var rule in accessRules)
            {
                if (rule.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(rule.UserId.Value);
                    if (user != null)
                    {
                        result.Add(new UserAccessInfo
                        {
                            UserId = user.Id,
                            UserName = user.FullName,
                            UserEmail = user.Email,
                            AccessLevel = rule.AccessLevel,
                            GrantedAt = rule.CreatedAt,
                            ExpiresAt = rule.ExpiresAt,
                            IsActive = rule.IsActive
                        });
                    }
                }
            }

            return result;
        }

        private async Task ApplyRightsToChildrenAsync(int folderId, int userId, AccessLevel accessLevel, int grantedBy)
        {
            // Получаем все дочерние папки
            var childFolders = await _folderRepository.GetByParentIdAsync(folderId);
            foreach (var childFolder in childFolders)
            {
                await _accessControlService.SetFolderAccessAsync(childFolder.Id, userId, accessLevel, grantedBy);
                await ApplyRightsToChildrenAsync(childFolder.Id, userId, accessLevel, grantedBy);
            }

            // Получаем все файлы в папке
            var files = await _fileRepository.GetByFolderIdAsync(folderId);
            foreach (var file in files)
            {
                await _accessControlService.SetFileAccessAsync(file.Id, userId, accessLevel, grantedBy);
            }
        }

        private string GetAccessLevelText(AccessLevel level)
        {
            return level switch
            {
                AccessLevel.None => "Нет доступа",
                AccessLevel.Read => "Только чтение",
                AccessLevel.Write => "Чтение и запись",
                AccessLevel.Delete => "Полный доступ",
                AccessLevel.Admin => "Администратор",
                _ => "Неизвестно"
            };
        }
    }

    public class SetFileAccessRequest
    {
        public int FileId { get; set; }
        public int UserId { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public int GrantedBy { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }

    public class SetFolderAccessRequest
    {
        public int FolderId { get; set; }
        public int UserId { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public int GrantedBy { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public bool ApplyToChildren { get; set; } = false;
    }

    public class AccessRightsResult
    {
        public int EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int GrantedBy { get; set; }
        public bool Success { get; set; }
        public AccessLevel NewAccessLevel { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserAccessInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public AccessLevel AccessLevel { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string AccessLevelText => GetAccessLevelText(AccessLevel);

        private string GetAccessLevelText(AccessLevel level)
        {
            return level switch
            {
                AccessLevel.None => "Нет доступа",
                AccessLevel.Read => "Только чтение",
                AccessLevel.Write => "Чтение и запись",
                AccessLevel.Delete => "Полный доступ",
                AccessLevel.Admin => "Администратор",
                _ => "Неизвестно"
            };
        }
    }
}