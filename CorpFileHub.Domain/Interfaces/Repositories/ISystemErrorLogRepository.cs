using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Domain.Interfaces.Repositories
{
    public interface ISystemErrorLogRepository
    {
        Task<SystemErrorLog> CreateAsync(SystemErrorLog log);
        Task<IEnumerable<SystemErrorLog>> GetRecentAsync(int count = 100);
    }
}
