using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;

namespace CorpFileHub.Application.UseCases.Access
{
    public class CheckAccessUseCase
    {
        private readonly IAccessControlService _accessControlService;
        private readonly IAuditLogRepository _auditLogRepository;

        public CheckAccessUseCase(
            IAccessControlService accessControlService,
            IAuditLogRepository auditLogRepository)
        {
            _accessControlService = accessControlService;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<AccessCheckResult> CheckFileAccessAsync(int fileId, int userId, AccessType accessType)
        {
            var result = new AccessCheckResult
            {
                EntityId = fileId,
                EntityType = "File",
                UserId = userId,
                AccessType = accessType,
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                result.HasAccess = accessType switch
                {
                    AccessType.Read => await _accessControlService.CanReadFileAsync(fileId, userId),
                    AccessType.Edit => await _accessControlService.CanEditFileAsync(fileId, userId),
                    AccessType.Delete => await _accessControlService.CanDeleteFileAsync(fileId, userId),
                    AccessType.ViewHistory => await _accessControlService.CanViewFileHistoryAsync(fileId, userId),
                    _ => false
                };

                result.AccessLevel = await _accessControlService.GetFileAccessLevelAsync(fileId, userId);

                // Логируем проверку доступа в аудит (только при отказе)
                if (!result.HasAccess)
                {
                    await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                    {
                        UserId = userId,
                        Action = AuditAction.AccessRightsView,
                        EntityType = "File",
                        EntityId = fileId,
                        Description = $"Отказ в доступе ({accessType}) к файлу",
                        IsSuccess = false,
                        ErrorMessage = "Недостаточно прав доступа"
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                result.HasAccess = false;
                result.ErrorMessage = ex.Message;

                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = userId,
                    Action = AuditAction.AccessRightsView,
                    EntityType = "File",
                    EntityId = fileId,
                    Description = $"Ошибка проверки доступа к файлу",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });

                return result;
            }
        }

        public async Task<AccessCheckResult> CheckFolderAccessAsync(int folderId, int userId, AccessType accessType)
        {
            var result = new AccessCheckResult
            {
                EntityId = folderId,
                EntityType = "Folder",
                UserId = userId,
                AccessType = accessType,
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                result.HasAccess = accessType switch
                {
                    AccessType.Read => await _accessControlService.CanReadFolderAsync(folderId, userId),
                    AccessType.Edit => await _accessControlService.CanEditFolderAsync(folderId, userId),
                    AccessType.Delete => await _accessControlService.CanDeleteFolderAsync(folderId, userId),
                    AccessType.Create => await _accessControlService.CanCreateInFolderAsync(folderId, userId),
                    _ => false
                };

                result.AccessLevel = await _accessControlService.GetFolderAccessLevelAsync(folderId, userId);

                // Логируем проверку доступа в аудит (только при отказе)
                if (!result.HasAccess)
                {
                    await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                    {
                        UserId = userId,
                        Action = AuditAction.AccessRightsView,
                        EntityType = "Folder",
                        EntityId = folderId,
                        Description = $"Отказ в доступе ({accessType}) к папке",
                        IsSuccess = false,
                        ErrorMessage = "Недостаточно прав доступа"
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                result.HasAccess = false;
                result.ErrorMessage = ex.Message;

                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = userId,
                    Action = AuditAction.AccessRightsView,
                    EntityType = "Folder",
                    EntityId = folderId,
                    Description = $"Ошибка проверки доступа к папке",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });

                return result;
            }
        }

        public async Task<UserAccessSummary> GetUserAccessSummaryAsync(int userId)
        {
            // Получаем сводку доступов пользователя
            var summary = new UserAccessSummary
            {
                UserId = userId,
                GeneratedAt = DateTime.UtcNow
            };

            // TODO: Реализовать получение полной сводки доступов
            // Здесь нужно будет собрать информацию о всех файлах и папках,
            // к которым у пользователя есть доступ

            return summary;
        }
    }

    public class AccessCheckResult
    {
        public int EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int UserId { get; set; }
        public AccessType AccessType { get; set; }
        public bool HasAccess { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public DateTime CheckedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class UserAccessSummary
    {
        public int UserId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<FileAccessInfo> FileAccess { get; set; } = new();
        public List<FolderAccessInfo> FolderAccess { get; set; } = new();
        public int TotalFiles { get; set; }
        public int TotalFolders { get; set; }
        public int ReadOnlyFiles { get; set; }
        public int EditableFiles { get; set; }
        public int AdminFiles { get; set; }
    }

    public class FileAccessInfo
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public AccessLevel AccessLevel { get; set; }
        public bool IsOwner { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class FolderAccessInfo
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public AccessLevel AccessLevel { get; set; }
        public bool IsOwner { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public enum AccessType
    {
        Read,
        Edit,
        Delete,
        Create,
        ViewHistory,
        ChangeAccess
    }
}