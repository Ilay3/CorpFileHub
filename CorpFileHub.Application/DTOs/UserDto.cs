namespace CorpFileHub.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        // Статистика пользователя
        public int OwnedFilesCount { get; set; }
        public int OwnedFoldersCount { get; set; }
        public long TotalFilesSize { get; set; }
        public DateTime? LastFileActivity { get; set; }

        // Для отображения
        public string InitialsAvatar => GetInitials(FullName);
        public string FormattedTotalSize => FormatFileSize(TotalFilesSize);
        public string LastActivityText => GetLastActivityText();
        public string StatusBadge => IsActive ? "success" : "danger";
        public string StatusText => IsActive ? "Активен" : "Заблокирован";
        public string RoleText => IsAdmin ? "Администратор" : "Пользователь";

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "?";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();

            return fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : fullName.ToUpper();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        private string GetLastActivityText()
        {
            if (!LastFileActivity.HasValue)
                return "Нет активности";

            var diff = DateTime.UtcNow - LastFileActivity.Value;

            if (diff.TotalMinutes < 1)
                return "Только что";
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} мин. назад";
            if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours} ч. назад";
            if (diff.TotalDays < 30)
                return $"{(int)diff.TotalDays} дн. назад";

            return LastFileActivity.Value.ToString("dd.MM.yyyy");
        }
    }

    public class UserCreateDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
    }

    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }

        // Статистика активности
        public int FilesCreated { get; set; }
        public int FoldersCreated { get; set; }
        public int FilesEdited { get; set; }
        public int FilesDownloaded { get; set; }
        public long TotalUploadedSize { get; set; }

        public string FormattedUploadedSize => FormatFileSize(TotalUploadedSize);

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }
    }
}