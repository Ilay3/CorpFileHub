namespace CorpFileHub.Domain.Entities
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    }
}