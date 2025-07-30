namespace CorpFileHub.Domain.Entities
{
    public class LoginLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public bool IsLogin { get; set; }
        public bool IsSuccess { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User? User { get; set; }
    }
}
