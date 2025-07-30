using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorpFileHub.Infrastructure.Repositories
{
    public class UnauthorizedAccessLogRepository : IUnauthorizedAccessLogRepository
    {
        private readonly ApplicationDbContext _context;
        public UnauthorizedAccessLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<UnauthorizedAccessLog> CreateAsync(UnauthorizedAccessLog log)
        {
            log.CreatedAt = DateTime.UtcNow;
            _context.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }
        public async Task<IEnumerable<UnauthorizedAccessLog>> GetRecentAsync(int count = 100)
        {
            return await _context.Set<UnauthorizedAccessLog>()
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
