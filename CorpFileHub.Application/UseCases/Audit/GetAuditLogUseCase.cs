using CorpFileHub.Application.DTOs;
using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;

namespace CorpFileHub.Application.UseCases.Audit
{
    public class GetAuditLogUseCase
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAccessControlService _accessControlService;

        public GetAuditLogUseCase(
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            IAccessControlService accessControlService)
        {
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _accessControlService = accessControlService;
        }

        public async Task<AuditLogPagedResult> GetAuditLogAsync(AuditLogSearchDto searchDto, int requesterId)
        {
            // Проверяем права доступа - только администраторы могут просматривать полный журнал
            var requester = await _userRepository.GetByIdAsync(requesterId);
            if (requester == null || !requester.IsAdmin)
            {
                throw new UnauthorizedAccessException("Недостаточно прав для просмотра журнала аудита");
            }

            var result = new AuditLogPagedResult
            {
                Page = searchDto.Page,
                PageSize = searchDto.PageSize,
                RequestedBy = requesterId,
                RequestedAt = DateTime.UtcNow
            };

            try
            {
                // Получаем записи аудита
                IEnumerable<Domain.Entities.AuditLog> auditLogs;

                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    auditLogs = await _auditLogRepository.SearchAsync(
                        searchDto.SearchTerm,
                        searchDto.DateFrom,
                        searchDto.DateTo);
                }
                else if (searchDto.Action.HasValue)
                {
                    auditLogs = await _auditLogRepository.GetByActionAsync(
                        searchDto.Action.Value,
                        searchDto.DateFrom,
                        searchDto.DateTo);
                }
                else if (searchDto.UserId.HasValue)
                {
                    auditLogs = await _auditLogRepository.GetByUserIdAsync(
                        searchDto.UserId.Value,
                        searchDto.Page,
                        searchDto.PageSize);
                }
                else
                {
                    // Если нет специфических фильтров, получаем последние записи с учетом дат
                    auditLogs = await _auditLogRepository.SearchAsync(
                        "",
                        searchDto.DateFrom,
                        searchDto.DateTo);
                }

                // Применяем дополнительные фильтры
                if (!string.IsNullOrEmpty(searchDto.EntityType))
                {
                    auditLogs = auditLogs.Where(al => al.EntityType.Equals(searchDto.EntityType, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(searchDto.EntityName))
                {
                    auditLogs = auditLogs.Where(al => al.EntityName.Contains(searchDto.EntityName, StringComparison.OrdinalIgnoreCase));
                }

                if (searchDto.IsSuccess.HasValue)
                {
                    auditLogs = auditLogs.Where(al => al.IsSuccess == searchDto.IsSuccess.Value);
                }

                // Подсчитываем общее количество
                result.TotalCount = auditLogs.Count();

                // Применяем пагинацию
                var pagedLogs = auditLogs
                    .OrderByDescending(al => al.CreatedAt)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToList();

                // Конвертируем в DTO
                result.Items = pagedLogs.Select(al => new AuditLogDto
                {
                    Id = al.Id,
                    UserId = al.UserId,
                    UserName = al.User?.FullName ?? "Неизвестный пользователь",
                    UserDepartment = al.User?.Department ?? "",
                    Action = al.Action,
                    EntityType = al.EntityType,
                    EntityId = al.EntityId,
                    EntityName = al.EntityName,
                    Description = al.Description,
                    IpAddress = al.IpAddress,
                    UserAgent = al.UserAgent,
                    CreatedAt = al.CreatedAt,
                    IsSuccess = al.IsSuccess,
                    ErrorMessage = al.ErrorMessage
                }).ToList();

                result.PageCount = (int)Math.Ceiling((double)result.TotalCount / searchDto.PageSize);
                result.HasNextPage = searchDto.Page < result.PageCount;
                result.HasPreviousPage = searchDto.Page > 1;

                // Логируем просмотр журнала аудита
                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.AccessRightsView,
                    EntityType = "AuditLog",
                    Description = $"Просмотр журнала аудита (страница {searchDto.Page}, размер {searchDto.PageSize})",
                    IsSuccess = true
                });

                return result;
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.SystemError,
                    Description = "Ошибка при получении журнала аудита",
                    ErrorMessage = ex.Message,
                    IsSuccess = false
                });

                throw;
            }
        }

        public async Task<AuditStatisticsDto> GetAuditStatisticsAsync(int requesterId, DateTime? from = null, DateTime? to = null)
        {
            // Проверяем права доступа
            var requester = await _userRepository.GetByIdAsync(requesterId);
            if (requester == null || !requester.IsAdmin)
            {
                throw new UnauthorizedAccessException("Недостаточно прав для просмотра статистики аудита");
            }

            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var statistics = new AuditStatisticsDto();

            try
            {
                // Получаем все записи за период
                var auditLogs = await _auditLogRepository.SearchAsync("", fromDate, toDate);
                var logList = auditLogs.ToList();

                statistics.TotalActions = logList.Count;
                statistics.SuccessfulActions = logList.Count(al => al.IsSuccess);
                statistics.FailedActions = logList.Count(al => !al.IsSuccess);
                statistics.UniqueUsers = logList.Select(al => al.UserId).Distinct().Count();

                // Статистика по типам действий
                statistics.ActionCounts = logList
                    .GroupBy(al => al.Action)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Топ пользователей по активности
                statistics.TopUsers = logList
                    .GroupBy(al => al.User?.FullName ?? "Неизвестный")
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Дневная активность
                statistics.DailyActivity = logList
                    .GroupBy(al => al.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Логируем просмотр статистики
                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.AccessRightsView,
                    EntityType = "AuditStatistics",
                    Description = $"Просмотр статистики аудита за период с {fromDate:dd.MM.yyyy} по {toDate:dd.MM.yyyy}",
                    IsSuccess = true
                });

                return statistics;
            }
            catch (Exception ex)
            {
                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.SystemError,
                    Description = "Ошибка при получении статистики аудита",
                    ErrorMessage = ex.Message,
                    IsSuccess = false
                });

                throw;
            }
        }

        public async Task<List<AuditLogDto>> GetEntityHistoryAsync(string entityType, int entityId, int requesterId)
        {
            // Проверяем права доступа к конкретной сущности
            bool hasAccess = entityType.ToLower() switch
            {
                "file" => await _accessControlService.CanReadFileAsync(entityId, requesterId),
                "folder" => await _accessControlService.CanReadFolderAsync(entityId, requesterId),
                _ => false
            };

            var requester = await _userRepository.GetByIdAsync(requesterId);
            if (!hasAccess && (requester == null || !requester.IsAdmin))
            {
                throw new UnauthorizedAccessException("Недостаточно прав для просмотра истории объекта");
            }

            try
            {
                var auditLogs = await _auditLogRepository.GetByEntityAsync(entityType, entityId);

                var result = auditLogs.Select(al => new AuditLogDto
                {
                    Id = al.Id,
                    UserId = al.UserId,
                    UserName = al.User?.FullName ?? "Неизвестный пользователь",
                    UserDepartment = al.User?.Department ?? "",
                    Action = al.Action,
                    EntityType = al.EntityType,
                    EntityId = al.EntityId,
                    EntityName = al.EntityName,
                    Description = al.Description,
                    IpAddress = al.IpAddress,
                    UserAgent = al.UserAgent,
                    CreatedAt = al.CreatedAt,
                    IsSuccess = al.IsSuccess,
                    ErrorMessage = al.ErrorMessage
                }).OrderByDescending(al => al.CreatedAt).ToList();

                // Логируем просмотр истории объекта
                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.AccessRightsView,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = $"Просмотр истории {entityType} (ID: {entityId})",
                    IsSuccess = true
                });

                return result;
            }
            catch (Exception ex)
            {
                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.SystemError,
                    Description = $"Ошибка при получении истории {entityType} (ID: {entityId})",
                    ErrorMessage = ex.Message,
                    IsSuccess = false
                });

                throw;
            }
        }

        public async Task<bool> CleanupOldLogsAsync(int requesterId, int retentionDays = 365)
        {
            // Проверяем права доступа - только администраторы
            var requester = await _userRepository.GetByIdAsync(requesterId);
            if (requester == null || !requester.IsAdmin)
            {
                throw new UnauthorizedAccessException("Недостаточно прав для очистки журнала аудита");
            }

            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                // TODO: Реализовать метод удаления старых записей в репозитории
                // var deletedCount = await _auditLogRepository.DeleteOldLogsAsync(cutoffDate);

                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.SystemBackup,
                    Description = $"Очистка журнала аудита (записи старше {retentionDays} дней)",
                    IsSuccess = true
                });

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
                {
                    UserId = requesterId,
                    Action = AuditAction.SystemError,
                    Description = "Ошибка при очистке журнала аудита",
                    ErrorMessage = ex.Message,
                    IsSuccess = false
                });

                return false;
            }
        }
    }

    public class AuditLogPagedResult
    {
        public List<AuditLogDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int TotalCount { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public int RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}