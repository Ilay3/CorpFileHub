namespace CorpFileHub.Application.DTOs
{
    public class FolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;

        // Иерархия
        public int? ParentFolderId { get; set; }
        public string? ParentFolderName { get; set; }
        public List<FolderDto> SubFolders { get; set; } = new();
        public int SubFoldersCount { get; set; }
        public int FilesCount { get; set; }
        public long TotalSize { get; set; }

        // Владелец
        public string OwnerName { get; set; } = string.Empty;

        // Права доступа для текущего пользователя
        public bool CanRead { get; set; } = true;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool CanCreateFiles { get; set; } = false;
        public bool CanCreateFolders { get; set; } = false;

        // Дополнительная информация
        public bool HasSubFolders => SubFoldersCount > 0;
        public bool HasFiles => FilesCount > 0;
        public bool IsEmpty => SubFoldersCount == 0 && FilesCount == 0;
        public string FormattedSize => FormatFileSize(TotalSize);

        // Для отображения в дереве
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public int Level { get; set; } = 0;

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

    public class FolderCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
    }

    public class FolderUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
    }

    public class FolderMoveDto
    {
        public int FolderId { get; set; }
        public int? NewParentFolderId { get; set; }
    }

    public class FolderTreeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
        public List<FolderTreeDto> Children { get; set; } = new();
        public bool HasFiles { get; set; }
        public int FilesCount { get; set; }
        public int SubFoldersCount { get; set; }
        public bool CanAccess { get; set; } = true;

        // Для UI дерева
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public int Level { get; set; } = 0;
    }

    public class BreadcrumbDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsClickable { get; set; } = true;
    }
}