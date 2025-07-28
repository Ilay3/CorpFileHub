using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public AuditAction Action { get; set; }
        public string EntityType { get; set; } = string.Empty; // "File", "Folder", "User"
        public int? EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        // Навигационные свойства
        public virtual User User { get; set; } = null!;
    }
}