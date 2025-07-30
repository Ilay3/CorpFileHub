using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorpFileHub.Infrastructure.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly ApplicationDbContext _context;

        public FileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FileItem?> GetByIdAsync(int id)
        {
            return await _context.Files
                .Include(f => f.Owner)
                .Include(f => f.Folder)
                .Include(f => f.Versions)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<FileItem?> GetByPathAsync(string path)
        {
            return await _context.Files
                .Include(f => f.Owner)
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Path == path && !f.IsDeleted);
        }

        public async Task<IEnumerable<FileItem>> GetByFolderIdAsync(int folderId)
        {
            return await _context.Files
                .Include(f => f.Owner)
                .Where(f => f.FolderId == folderId && !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileItem>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Files
                .Include(f => f.Folder)
                .Where(f => f.OwnerId == ownerId && !f.IsDeleted)
                .OrderByDescending(f => f.UpdatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileItem>> SearchAsync(string searchTerm, int? folderId = null)
        {
            var query = _context.Files
                .Include(f => f.Owner)
                .Include(f => f.Folder)
                .Where(f => !f.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f => f.Name.Contains(searchTerm) ||
                                        f.Description.Contains(searchTerm) ||
                                        f.Tags.Contains(searchTerm));
            }

            if (folderId.HasValue)
            {
                query = query.Where(f => f.FolderId == folderId.Value);
            }

            return await query.OrderBy(f => f.Name).ToListAsync();
        }

        public async Task<IEnumerable<FileItem>> SearchAdvancedAsync(
            string? searchTerm,
            int? folderId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? extension = null,
            int? ownerId = null,
            string? tags = null,
            long? minSize = null,
            long? maxSize = null)
        {
            var query = _context.Files
                .Include(f => f.Owner)
                .Include(f => f.Folder)
                .Where(f => !f.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f => f.Name.Contains(searchTerm) ||
                                        f.Description.Contains(searchTerm) ||
                                        f.Tags.Contains(searchTerm));
            }

            if (folderId.HasValue)
            {
                query = query.Where(f => f.FolderId == folderId.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(f => f.CreatedAt >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(f => f.CreatedAt <= dateTo.Value);
            }

            if (!string.IsNullOrEmpty(extension))
            {
                query = query.Where(f => f.Extension.ToLower() == extension.ToLower());
            }

            if (ownerId.HasValue)
            {
                query = query.Where(f => f.OwnerId == ownerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(t => t.Trim());
                foreach (var tag in tagList)
                {
                    var t = tag;
                    query = query.Where(f => f.Tags.Contains(t));
                }
            }

            if (minSize.HasValue)
            {
                query = query.Where(f => f.Size >= minSize.Value);
            }

            if (maxSize.HasValue)
            {
                query = query.Where(f => f.Size <= maxSize.Value);
            }

            return await query.OrderBy(f => f.Name).ToListAsync();
        }

        public async Task<FileItem> CreateAsync(FileItem file)
        {
            file.CreatedAt = DateTime.UtcNow;
            file.UpdatedAt = DateTime.UtcNow;

            _context.Files.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<FileItem> UpdateAsync(FileItem file)
        {
            file.UpdatedAt = DateTime.UtcNow;

            _context.Files.Update(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null) return false;

            file.IsDeleted = true;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FileItem?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Files
                .Include(f => f.Owner)
                .Include(f => f.Folder)
                .Include(f => f.Versions)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<bool> FileExistsAsync(string name, int folderId)
        {
            return await _context.Files
                .AnyAsync(f => f.Name == name && f.FolderId == folderId && !f.IsDeleted);
        }

        public async Task<IEnumerable<FileItem>> GetByStatusAsync(FileStatus status)
        {
            return await _context.Files
                .Include(f => f.Owner)
                .Include(f => f.Folder)
                .Where(f => f.Status == status && !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileItem>> GetAllWithVersionsAsync()
        {
            return await _context.Files
                .Include(f => f.Versions)
                .Where(f => !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<bool> DeleteFileVersionAsync(int versionId)
        {
            var version = await _context.FileVersions.FindAsync(versionId);
            if (version == null) return false;

            _context.FileVersions.Remove(version);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
