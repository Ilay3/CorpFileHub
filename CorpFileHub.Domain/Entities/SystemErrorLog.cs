namespace CorpFileHub.Domain.Entities
{
    public class SystemErrorLog
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
