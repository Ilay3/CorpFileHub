﻿namespace CorpFileHub.Domain.Entities
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string YandexDiskPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string Description { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;

        // Внешние ключи
        public int? ParentFolderId { get; set; }
        public int OwnerId { get; set; }

        // Навигационные свойства
        public virtual Folder? ParentFolder { get; set; }
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public virtual ICollection<FileItem> Files { get; set; } = new List<FileItem>();
        public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    }
}