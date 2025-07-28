namespace CorpFileHub.Domain.Entities
{
    public class FileVersion
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public string LocalPath { get; set; } = string.Empty;
        public string YandexDiskPath { get; set; } = string.Empty;
        public int Version { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual FileItem File { get; set; } = null!;
        public virtual User CreatedBy { get; set; } = null!;
    }
}