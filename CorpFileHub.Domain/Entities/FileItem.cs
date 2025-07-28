using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Domain.Entities
{
    public class FileItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string YandexDiskPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public FileStatus Status { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string Tags { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Внешние ключи
        public int OwnerId { get; set; }
        public int FolderId { get; set; }

        // Навигационные свойства
        public virtual User Owner { get; set; } = null!;
        public virtual Folder Folder { get; set; } = null!;
        public virtual ICollection<FileVersion> Versions { get; set; } = new List<FileVersion>();
        public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    }
}