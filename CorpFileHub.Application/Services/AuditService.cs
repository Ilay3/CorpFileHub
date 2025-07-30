using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace CorpFileHub.Application.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(int userId, AuditAction action, string entityType, int? entityId = null,
            string entityName = "", string description = "", string? comment = null);

        Task LogSuccessAsync(int userId, AuditAction action, string entityType, int? entityId = null,
            string entityName = "", string description = "");

        Task LogErrorAsync(int userId, AuditAction action, string entityType, int? entityId = null,
            string entityName = "", string description = "", string errorMessage = "");

        Task LogSystemActionAsync(AuditAction action, string description = "", string? errorMessage = null);

        Task LogLoginAttemptAsync(int? userId, bool isLogin, bool isSuccess, string? errorMessage = null);

        Task LogUnauthorizedAttemptAsync(int? userId, string action, string entityType, int? entityId = null, string description = "");

        Task LogSystemErrorAsync(string message, string? details = null);

        Task<bool> CleanupOldLogsAsync(int retentionDays = 365);
    }

    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IAuthLogRepository _authLogRepository;
        private readonly IUnauthorizedAccessLogRepository _unauthorizedRepository;
        private readonly ISystemErrorLogRepository _systemErrorRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IAuditLogRepository auditLogRepository,
            IAuthLogRepository authLogRepository,
            IUnauthorizedAccessLogRepository unauthorizedRepository,
            ISystemErrorLogRepository systemErrorRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _auditLogRepository = auditLogRepository;
            _authLogRepository = authLogRepository;
            _unauthorizedRepository = unauthorizedRepository;
            _systemErrorRepository = systemErrorRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActionAsync(int userId, AuditAction action, string entityType,
            int? entityId = null, string entityName = "", string description = "", string? comment = null)
        {
            try
            {
                var auditLog = CreateAuditLog(userId, action, entityType, entityId, entityName, description, true);

                if (!string.IsNullOrEmpty(comment))
                {
                    auditLog.Description += $" Комментарий: {comment}";
                }

                await _auditLogRepository.CreateAsync(auditLog);

                _logger.LogInformation("Аудит: {Action} - {Description} (Пользователь: {UserId})",
                    action, description, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи в журнал аудита");
            }
        }

        public async Task LogSuccessAsync(int userId, AuditAction action, string entityType,
            int? entityId = null, string entityName = "", string description = "")
        {
            try
            {
                var auditLog = CreateAuditLog(userId, action, entityType, entityId, entityName, description, true);
                await _auditLogRepository.CreateAsync(auditLog);

                _logger.LogInformation("Успешное действие: {Action} - {Description} (Пользователь: {UserId})",
                    action, description, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи успешного действия в аудит");
            }
        }

        public async Task LogErrorAsync(int userId, AuditAction action, string entityType,
            int? entityId = null, string entityName = "", string description = "", string errorMessage = "")
        {
            try
            {
                var auditLog = CreateAuditLog(userId, action, entityType, entityId, entityName, description, false);
                auditLog.ErrorMessage = errorMessage;

                await _auditLogRepository.CreateAsync(auditLog);

                _logger.LogWarning("Ошибка действия: {Action} - {Description} - {Error} (Пользователь: {UserId})",
                    action, description, errorMessage, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи ошибочного действия в аудит");
            }
        }

        public async Task LogSystemActionAsync(AuditAction action, string description = "", string? errorMessage = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = 0, // Системные действия от имени системы
                    Action = action,
                    EntityType = "System",
                    EntityName = "System",
                    Description = description,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    CreatedAt = DateTime.UtcNow,
                    IsSuccess = string.IsNullOrEmpty(errorMessage),
                    ErrorMessage = errorMessage ?? ""
                };

                await _auditLogRepository.CreateAsync(auditLog);

                var logLevel = string.IsNullOrEmpty(errorMessage) ? LogLevel.Information : LogLevel.Error;
                _logger.Log(logLevel, "Системное действие: {Action} - {Description}", action, description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи системного действия в аудит");
            }
        }

        public async Task LogLoginAttemptAsync(int? userId, bool isLogin, bool isSuccess, string? errorMessage = null)
        {
            try
            {
                var log = new LoginLog
                {
                    UserId = userId,
                    IsLogin = isLogin,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    CreatedAt = DateTime.UtcNow
                };

                await _authLogRepository.CreateAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи лога аутентификации");
            }
        }

        public async Task LogUnauthorizedAttemptAsync(int? userId, string action, string entityType, int? entityId = null, string description = "")
        {
            try
            {
                var log = new UnauthorizedAccessLog
                {
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    IpAddress = GetClientIpAddress(),
                    CreatedAt = DateTime.UtcNow
                };
                await _unauthorizedRepository.CreateAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи лога несанкционированного доступа");
            }
        }

        public async Task LogSystemErrorAsync(string message, string? details = null)
        {
            try
            {
                var log = new SystemErrorLog
                {
                    Message = message,
                    Details = details,
                    CreatedAt = DateTime.UtcNow
                };
                await _systemErrorRepository.CreateAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи системной ошибки");
            }
        }

        public async Task<bool> CleanupOldLogsAsync(int retentionDays = 365)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var deletedCount = await _auditLogRepository.DeleteOlderThanAsync(cutoffDate);

                await LogSystemActionAsync(AuditAction.SystemBackup,
                    $"Очистка журнала аудита (удалено {deletedCount} записей старше {retentionDays} дней)");

                _logger.LogInformation(
                    "Выполнена очистка журнала аудита: удалено {Count} записей старше {Days} дней",
                    deletedCount, retentionDays);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке журнала аудита");
                await LogSystemActionAsync(AuditAction.SystemError,
                    "Ошибка при очистке журнала аудита", ex.Message);
                return false;
            }
        }

        private AuditLog CreateAuditLog(int userId, AuditAction action, string entityType,
            int? entityId, string entityName, string description, bool isSuccess)
        {
            return new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                CreatedAt = DateTime.UtcNow,
                IsSuccess = isSuccess,
                ErrorMessage = ""
            };
        }

        private string GetClientIpAddress()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return "127.0.0.1";

                var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

                if (string.IsNullOrEmpty(ipAddress))
                    ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();

                if (string.IsNullOrEmpty(ipAddress))
                    ipAddress = context.Connection.RemoteIpAddress?.ToString();

                return ipAddress ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private string GetUserAgent()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                return context?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}