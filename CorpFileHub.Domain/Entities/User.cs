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

        public virtual ICollection<Files> OwnedFiles { get; set; } = new List<Files>();
        public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}