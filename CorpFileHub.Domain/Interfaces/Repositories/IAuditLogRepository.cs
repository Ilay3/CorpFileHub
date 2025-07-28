using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Domain.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> CreateAsync(AuditLog auditLog);
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 50);
        Task<IEnumerable<AuditLog>> GetByActionAsync(AuditAction action, DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId);
        Task<IEnumerable<AuditLog>> SearchAsync(string searchTerm, DateTime? from = null, DateTime? to = null);
    }
}