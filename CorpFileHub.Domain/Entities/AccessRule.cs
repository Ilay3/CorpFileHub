using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Domain.Entities
{
    public class AccessRule
    {
        public int Id { get; set; }
        public int? FileId { get; set; }
        public int? FolderId { get; set; }
        public int? UserId { get; set; }
        public int? GroupId { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int CreatedById { get; set; }
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual FileItem? File { get; set; }
        public virtual Folder? Folder { get; set; }
        public virtual User? User { get; set; }
        public virtual Group? Group { get; set; }
        public virtual User CreatedBy { get; set; } = null!;
    }
}