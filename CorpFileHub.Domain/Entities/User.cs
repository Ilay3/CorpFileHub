namespace CorpFileHub.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; } = false;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        // Навигационные свойства
        public virtual ICollection<FileItem> OwnedFiles { get; set; } = new List<FileItem>();
        public virtual ICollection<Folder> OwnedFolders { get; set; } = new List<Folder>();
        public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}