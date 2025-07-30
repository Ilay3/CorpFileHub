using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorpFileHub.Infrastructure.Repositories
{
    public class SystemErrorLogRepository : ISystemErrorLogRepository
    {
        private readonly ApplicationDbContext _context;
        public SystemErrorLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<SystemErrorLog> CreateAsync(SystemErrorLog log)
        {
            log.CreatedAt = DateTime.UtcNow;
            _context.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }
        public async Task<IEnumerable<SystemErrorLog>> GetRecentAsync(int count = 100)
        {
            return await _context.Set<SystemErrorLog>()
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
