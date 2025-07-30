using Microsoft.AspNetCore.SignalR;
using CorpFileHub.Presentation.Hubs;

namespace CorpFileHub.Presentation.Services
{
    public interface ISignalRService
    {
        Task NotifyFileUploadedAsync(int folderId, string fileName, string uploaderName);
        Task NotifyFileDeletedAsync(int folderId, int fileId, string fileName);
        Task NotifyFolderCreatedAsync(int? parentFolderId, string folderName, string creatorName);
        Task NotifyFolderDeletedAsync(int? parentFolderId, int folderId, string folderName);
        Task NotifyFileOpenedForEditingAsync(int fileId, string fileName, string editorName);
        Task NotifyFileEditingFinishedAsync(int fileId, string fileName);
        Task NotifyFolderMovedAsync(int folderId, int? oldParentId, int? newParentId);
        Task NotifyFileVersionRolledBackAsync(int fileId, string fileName, int targetVersion);
        Task NotifyAccessRightsChangedAsync(string entityType, int entityId, string entityName, string changedBy);
        Task NotifyUserLoginAsync(string userName, string department);
        Task NotifyUserLogoutAsync(string userName);
        Task SendProgressUpdateAsync(string connectionId, string operationId, int progress, string message = "");
        Task SendErrorNotificationAsync(string groupOrConnectionId, string errorMessage, string? details = null);
        Task SendSystemNotificationAsync(string message, string notificationType = "info");
    }

    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<FileOperationHub> _hubContext;
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(IHubContext<FileOperationHub> hubContext, ILogger<SignalRService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyFileUploadedAsync(int folderId, string fileName, string uploaderName)
        {
            try
            {
                var data = new
                {
                    FolderId = folderId,
                    FileName = fileName,
                    UploaderName = uploaderName,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"folder_{folderId}").SendAsync("FileUploaded", data);
                await _hubContext.Clients.All.SendAsync("FileUploaded", data);

                _logger.LogInformation("Уведомление о загрузке файла '{FileName}' отправлено", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о загрузке файла");
            }
        }

        public async Task NotifyFileDeletedAsync(int folderId, int fileId, string fileName)
        {
            try
            {
                var data = new
                {
                    FolderId = folderId,
                    FileId = fileId,
                    FileName = fileName,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"folder_{folderId}").SendAsync("FileDeleted", data);
                await _hubContext.Clients.All.SendAsync("FileDeleted", data);

                _logger.LogInformation("Уведомление об удалении файла '{FileName}' отправлено", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления об удалении файла");
            }
        }

        public async Task NotifyFolderCreatedAsync(int? parentFolderId, string folderName, string creatorName)
        {
            try
            {
                var data = new
                {
                    ParentFolderId = parentFolderId,
                    FolderName = folderName,
                    CreatorName = creatorName,
                    Timestamp = DateTime.UtcNow
                };

                var groupName = parentFolderId.HasValue ? $"folder_{parentFolderId}" : "root_folder";
                await _hubContext.Clients.Group(groupName).SendAsync("FolderCreated", data);
                await _hubContext.Clients.All.SendAsync("FolderCreated", data);

                _logger.LogInformation("Уведомление о создании папки '{FolderName}' отправлено", folderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о создании папки");
            }
        }

        public async Task NotifyFolderDeletedAsync(int? parentFolderId, int folderId, string folderName)
        {
            try
            {
                var data = new
                {
                    ParentFolderId = parentFolderId,
                    FolderId = folderId,
                    FolderName = folderName,
                    Timestamp = DateTime.UtcNow
                };

                var groupName = parentFolderId.HasValue ? $"folder_{parentFolderId}" : "root_folder";
                await _hubContext.Clients.Group(groupName).SendAsync("FolderDeleted", data);

                _logger.LogInformation("Уведомление об удалении папки '{FolderName}' отправлено", folderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления об удалении папки");
            }
        }

        public async Task NotifyFileOpenedForEditingAsync(int fileId, string fileName, string editorName)
        {
            try
            {
                var data = new
                {
                    FileId = fileId,
                    FileName = fileName,
                    EditorName = editorName,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("FileOpenedForEditing", data);

                _logger.LogInformation("Уведомление об открытии файла '{FileName}' для редактирования отправлено", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления об открытии файла для редактирования");
            }
        }

        public async Task NotifyFileEditingFinishedAsync(int fileId, string fileName)
        {
            try
            {
                var data = new
                {
                    FileId = fileId,
                    FileName = fileName,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("FileEditingFinished", data);

                _logger.LogInformation("Уведомление о завершении редактирования файла '{FileName}' отправлено", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о завершении редактирования файла");
            }
        }

        public async Task NotifyFolderMovedAsync(int folderId, int? oldParentId, int? newParentId)
        {
            try
            {
                var data = new
                {
                    FolderId = folderId,
                    OldParentId = oldParentId,
                    NewParentId = newParentId,
                    Timestamp = DateTime.UtcNow
                };

                // Уведомляем группы старой и новой родительских папок
                if (oldParentId.HasValue)
                    await _hubContext.Clients.Group($"folder_{oldParentId}").SendAsync("FolderMoved", data);

                if (newParentId.HasValue)
                    await _hubContext.Clients.Group($"folder_{newParentId}").SendAsync("FolderMoved", data);

                await _hubContext.Clients.All.SendAsync("FolderMoved", data);

                _logger.LogInformation("Уведомление о перемещении папки {FolderId} отправлено", folderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о перемещении папки");
            }
        }

        public async Task NotifyFileVersionRolledBackAsync(int fileId, string fileName, int targetVersion)
        {
            try
            {
                var data = new
                {
                    FileId = fileId,
                    FileName = fileName,
                    TargetVersion = targetVersion,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("FileVersionRolledBack", data);

                _logger.LogInformation("Уведомление об откате версии файла '{FileName}' к версии {Version} отправлено",
                    fileName, targetVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления об откате версии файла");
            }
        }

        public async Task NotifyAccessRightsChangedAsync(string entityType, int entityId, string entityName, string changedBy)
        {
            try
            {
                var data = new
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    EntityName = entityName,
                    ChangedBy = changedBy,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("AccessRightsChanged", data);

                _logger.LogInformation("Уведомление об изменении прав доступа к {EntityType} '{EntityName}' отправлено",
                    entityType, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления об изменении прав доступа");
            }
        }

        public async Task NotifyUserLoginAsync(string userName, string department)
        {
            try
            {
                var data = new
                {
                    UserName = userName,
                    Department = department,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("UserLogin", data);

                _logger.LogInformation("Уведомление о входе пользователя '{UserName}' отправлено", userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о входе пользователя");
            }
        }

        public async Task NotifyUserLogoutAsync(string userName)
        {
            try
            {
                var data = new
                {
                    UserName = userName,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("UserLogout", data);

                _logger.LogInformation("Уведомление о выходе пользователя '{UserName}' отправлено", userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о выходе пользователя");
            }
        }

        public async Task SendProgressUpdateAsync(string connectionId, string operationId, int progress, string message = "")
        {
            try
            {
                var data = new
                {
                    OperationId = operationId,
                    Progress = progress,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Client(connectionId).SendAsync("ProgressUpdate", data);

                _logger.LogDebug("Обновление прогресса {Progress}% для операции {OperationId} отправлено",
                    progress, operationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки обновления прогресса");
            }
        }

        public async Task SendErrorNotificationAsync(string groupOrConnectionId, string errorMessage, string? details = null)
        {
            try
            {
                var data = new
                {
                    ErrorMessage = errorMessage,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                // Пытаемся определить, это группа или ID подключения
                if (groupOrConnectionId.StartsWith("folder_") || groupOrConnectionId == "all")
                {
                    var clients = groupOrConnectionId == "all"
                        ? _hubContext.Clients.All
                        : _hubContext.Clients.Group(groupOrConnectionId);

                    await clients.SendAsync("ErrorNotification", data);
                }
                else
                {
                    await _hubContext.Clients.Client(groupOrConnectionId).SendAsync("ErrorNotification", data);
                }

                _logger.LogWarning("Уведомление об ошибке отправлено: {ErrorMessage}", errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления об ошибке");
            }
        }

        public async Task SendSystemNotificationAsync(string message, string notificationType = "info")
        {
            try
            {
                var data = new
                {
                    Message = message,
                    Type = notificationType,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("SystemNotification", data);

                _logger.LogInformation("Системное уведомление отправлено: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки системного уведомления");
            }
        }
    }
}