using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorpFileHub.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLog> CreateAsync(AuditLog auditLog)
        {
            auditLog.CreatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByActionAsync(AuditAction action, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.Action == action);

            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.CreatedAt <= to.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> SearchAsync(string searchTerm, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a =>
                    a.EntityName.Contains(searchTerm) ||
                    a.Description.Contains(searchTerm) ||
                    a.User.FullName.Contains(searchTerm));
            }

            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.CreatedAt <= to.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate)
        {
            var oldLogs = _context.AuditLogs.Where(a => a.CreatedAt < cutoffDate);
            var count = await oldLogs.CountAsync();
            if (count > 0)
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
            }
            return count;
        }
    }
}