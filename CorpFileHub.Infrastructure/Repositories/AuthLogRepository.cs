using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorpFileHub.Infrastructure.Repositories
{
    public class AuthLogRepository : IAuthLogRepository
    {
        private readonly ApplicationDbContext _context;
        public AuthLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<LoginLog> CreateAsync(LoginLog log)
        {
            log.CreatedAt = DateTime.UtcNow;
            _context.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }
        public async Task<IEnumerable<LoginLog>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 50)
        {
            return await _context.Set<LoginLog>()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
