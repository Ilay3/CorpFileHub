using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserDepartment { get; set; } = string.Empty;
        public AuditAction Action { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        // Для отображения в UI
        public string ActionText => GetActionText(Action);
        public string ActionIcon => GetActionIcon(Action);
        public string ActionBadgeClass => GetActionBadgeClass(Action);
        public string StatusIcon => IsSuccess ? "bi bi-check-circle text-success" : "bi bi-x-circle text-danger";
        public string StatusText => IsSuccess ? "Успешно" : "Ошибка";
        public string FormattedDate => CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");
        public string RelativeTime => GetRelativeTime(CreatedAt);
        public string ShortUserAgent => GetShortUserAgent(UserAgent);

        private string GetActionText(AuditAction action)
        {
            return action switch
            {
                AuditAction.Login => "Вход в систему",
                AuditAction.Logout => "Выход из системы",
                AuditAction.FileUpload => "Загрузка файла",
                AuditAction.FileDownload => "Скачивание файла",
                AuditAction.FileEdit => "Редактирование файла",
                AuditAction.FileDelete => "Удаление файла",
                AuditAction.FileRestore => "Восстановление файла",
                AuditAction.FileMove => "Перемещение файла",
                AuditAction.FileRename => "Переименование файла",
                AuditAction.FolderCreate => "Создание папки",
                AuditAction.FolderDelete => "Удаление папки",
                AuditAction.FolderMove => "Перемещение папки",
                AuditAction.FolderRename => "Переименование папки",
                AuditAction.VersionRollback => "Откат версии",
                AuditAction.VersionCreate => "Создание версии",
                AuditAction.VersionDelete => "Удаление версии",
                AuditAction.AccessRightsChange => "Изменение прав доступа",
                AuditAction.AccessRightsView => "Просмотр прав доступа",
                AuditAction.UserCreate => "Создание пользователя",
                AuditAction.UserUpdate => "Изменение пользователя",
                AuditAction.UserDelete => "Удаление пользователя",
                AuditAction.UserBlock => "Блокировка пользователя",
                AuditAction.UserUnblock => "Разблокировка пользователя",
                AuditAction.GroupCreate => "Создание группы",
                AuditAction.GroupUpdate => "Изменение группы",
                AuditAction.GroupDelete => "Удаление группы",
                AuditAction.GroupAddUser => "Добавление в группу",
                AuditAction.GroupRemoveUser => "Удаление из группы",
                AuditAction.SystemBackup => "Резервное копирование",
                AuditAction.SystemRestore => "Восстановление системы",
                AuditAction.SystemError => "Системная ошибка",
                AuditAction.Search => "Поиск",
                AuditAction.FileView => "Просмотр файла",
                AuditAction.FolderView => "Просмотр папки",
                _ => "Неизвестное действие"
            };
        }

        private string GetActionIcon(AuditAction action)
        {
            return action switch
            {
                AuditAction.Login => "bi bi-box-arrow-in-right",
                AuditAction.Logout => "bi bi-box-arrow-right",
                AuditAction.FileUpload => "bi bi-cloud-upload",
                AuditAction.FileDownload => "bi bi-cloud-download",
                AuditAction.FileEdit => "bi bi-pencil",
                AuditAction.FileDelete => "bi bi-trash",
                AuditAction.FileRestore => "bi bi-arrow-clockwise",
                AuditAction.FileMove => "bi bi-folder-symlink",
                AuditAction.FileRename => "bi bi-pencil-square",
                AuditAction.FolderCreate => "bi bi-folder-plus",
                AuditAction.FolderDelete => "bi bi-folder-x",
                AuditAction.FolderMove => "bi bi-folder-symlink",
                AuditAction.FolderRename => "bi bi-folder-fill",
                AuditAction.VersionRollback => "bi bi-arrow-counterclockwise",
                AuditAction.VersionCreate => "bi bi-plus-circle",
                AuditAction.VersionDelete => "bi bi-dash-circle",
                AuditAction.AccessRightsChange => "bi bi-shield-check",
                AuditAction.AccessRightsView => "bi bi-eye",
                AuditAction.UserCreate => "bi bi-person-plus",
                AuditAction.UserUpdate => "bi bi-person-gear",
                AuditAction.UserDelete => "bi bi-person-x",
                AuditAction.UserBlock => "bi bi-person-slash",
                AuditAction.UserUnblock => "bi bi-person-check",
                AuditAction.GroupCreate => "bi bi-people-fill",
                AuditAction.GroupUpdate => "bi bi-people",
                AuditAction.GroupDelete => "bi bi-people",
                AuditAction.GroupAddUser => "bi bi-person-plus-fill",
                AuditAction.GroupRemoveUser => "bi bi-person-dash-fill",
                AuditAction.SystemBackup => "bi bi-hdd",
                AuditAction.SystemRestore => "bi bi-hdd-fill",
                AuditAction.SystemError => "bi bi-exclamation-triangle",
                AuditAction.Search => "bi bi-search",
                AuditAction.FileView => "bi bi-eye",
                AuditAction.FolderView => "bi bi-folder-open",
                _ => "bi bi-question-circle"
            };
        }

        private string GetActionBadgeClass(AuditAction action)
        {
            return action switch
            {
                AuditAction.Login or AuditAction.FileUpload or AuditAction.FolderCreate or
                AuditAction.UserCreate or AuditAction.GroupCreate => "badge bg-success",

                AuditAction.FileDelete or AuditAction.FolderDelete or AuditAction.UserDelete or
                AuditAction.UserBlock or AuditAction.SystemError => "badge bg-danger",

                AuditAction.FileEdit or AuditAction.AccessRightsChange or AuditAction.UserUpdate or
                AuditAction.VersionRollback => "badge bg-warning text-dark",

                AuditAction.FileDownload or AuditAction.FileView or AuditAction.Search or
                AuditAction.Logout => "badge bg-info",

                _ => "badge bg-secondary"
            };
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;

            if (diff.TotalMinutes < 1)
                return "только что";
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} мин. назад";
            if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours} ч. назад";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} дн. назад";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)} нед. назад";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)} мес. назад";

            return $"{(int)(diff.TotalDays / 365)} г. назад";
        }

        private string GetShortUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Неизвестно";

            if (userAgent.Contains("Chrome"))
                return "Chrome";
            if (userAgent.Contains("Firefox"))
                return "Firefox";
            if (userAgent.Contains("Safari"))
                return "Safari";
            if (userAgent.Contains("Edge"))
                return "Edge";

            return "Другой браузер";
        }
    }

    public class AuditLogSearchDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? UserId { get; set; }
        public AuditAction? Action { get; set; }
        public string? EntityType { get; set; }
        public string? EntityName { get; set; }
        public bool? IsSuccess { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditStatisticsDto
    {
        public int TotalActions { get; set; }
        public int SuccessfulActions { get; set; }
        public int FailedActions { get; set; }
        public int UniqueUsers { get; set; }
        public Dictionary<AuditAction, int> ActionCounts { get; set; } = new();
        public Dictionary<string, int> TopUsers { get; set; } = new();
        public Dictionary<DateTime, int> DailyActivity { get; set; } = new();

        public double SuccessRate => TotalActions > 0 ? (double)SuccessfulActions / TotalActions * 100 : 0;
        public double FailureRate => TotalActions > 0 ? (double)FailedActions / TotalActions * 100 : 0;
    }
}