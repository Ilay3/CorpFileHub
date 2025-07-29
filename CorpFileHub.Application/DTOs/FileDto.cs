using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.DTOs
{
    public class FileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public FileStatus Status { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Связанные данные
        public string OwnerName { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public int FolderId { get; set; }
        public int VersionsCount { get; set; }
        public DateTime? LastEditedAt { get; set; }
        public string? LastEditedBy { get; set; }

        // Права доступа для текущего пользователя
        public bool CanRead { get; set; } = true;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool CanViewHistory { get; set; } = false;

        // Дополнительная информация
        public bool IsInEditing { get; set; } = false;
        public string? EditingByUser { get; set; }
        public string FormattedSize => FormatFileSize(Size);
        public string FileIcon => GetFileIcon(Extension);
        public bool CanEditOnline => CanEditOnlineCheck(Extension);

        private string FormatFileSize(long bytes)
        {
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

        private string GetFileIcon(string extension)
        {
            return extension.ToLower() switch
            {
                ".docx" or ".doc" => "bi bi-file-earmark-word text-primary",
                ".xlsx" or ".xls" => "bi bi-file-earmark-excel text-success",
                ".pptx" or ".ppt" => "bi bi-file-earmark-ppt text-warning",
                ".pdf" => "bi bi-file-earmark-pdf text-danger",
                ".txt" => "bi bi-file-earmark-text text-secondary",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "bi bi-file-earmark-image text-info",
                _ => "bi bi-file-earmark text-secondary"
            };
        }

        private bool CanEditOnlineCheck(string extension)
        {
            var editableExtensions = new[] { ".docx", ".xlsx", ".pptx" };
            return editableExtensions.Contains(extension.ToLower());
        }
    }

    public class FileVersionDto
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public int Version { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string FormattedSize => FormatFileSize(Size);

        private string FormatFileSize(long bytes)
        {
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

    public class FileUploadDto
    {
        public string Name { get; set; } = string.Empty;
        public int FolderId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    public class FileSearchDto
    {
        public string Query { get; set; } = string.Empty;
        public int? FolderId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Extension { get; set; }
        public string? Owner { get; set; }
        public string? Tags { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}