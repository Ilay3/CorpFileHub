using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Domain.Interfaces.Repositories
{
    public interface IAuthLogRepository
    {
        Task<LoginLog> CreateAsync(LoginLog log);
        Task<IEnumerable<LoginLog>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 50);
    }
}
