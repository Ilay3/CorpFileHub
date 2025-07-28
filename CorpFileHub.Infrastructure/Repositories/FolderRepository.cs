using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Entities;
using CorpFileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorpFileHub.Infrastructure.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly ApplicationDbContext _context;

        public FolderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Folder?> GetByIdAsync(int id)
        {
            return await _context.Folders
                .Include(f => f.Owner)
                .Include(f => f.ParentFolder)
                .Include(f => f.SubFolders.Where(sf => !sf.IsDeleted))
                .Include(f => f.Files.Where(file => !file.IsDeleted))
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<Folder?> GetByPathAsync(string path)
        {
            return await _context.Folders
                .Include(f => f.Owner)
                .Include(f => f.ParentFolder)
                .FirstOrDefaultAsync(f => f.Path == path && !f.IsDeleted);
        }

        public async Task<IEnumerable<Folder>> GetByParentIdAsync(int? parentId)
        {
            return await _context.Folders
                .Include(f => f.Owner)
                .Where(f => f.ParentFolderId == parentId && !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Folder>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Folders
                .Where(f => f.OwnerId == ownerId && !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        public async Task<Folder> CreateAsync(Folder folder)
        {
            folder.CreatedAt = DateTime.UtcNow;
            folder.UpdatedAt = DateTime.UtcNow;

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();
            return folder;
        }

        public async Task<Folder> UpdateAsync(Folder folder)
        {
            folder.UpdatedAt = DateTime.UtcNow;

            _context.Folders.Update(folder);
            await _context.SaveChangesAsync();
            return folder;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null) return false;

            folder.IsDeleted = true;
            folder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FolderExistsAsync(string name, int? parentId)
        {
            return await _context.Folders
                .AnyAsync(f => f.Name == name && f.ParentFolderId == parentId && !f.IsDeleted);
        }
    }
}