using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Domain.Interfaces.Repositories
{
    public interface IUnauthorizedAccessLogRepository
    {
        Task<UnauthorizedAccessLog> CreateAsync(UnauthorizedAccessLog log);
        Task<IEnumerable<UnauthorizedAccessLog>> GetRecentAsync(int count = 100);
    }
}
