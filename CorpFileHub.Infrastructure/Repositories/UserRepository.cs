using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Entities;
using CorpFileHub.Infrastructure.Data;

namespace CorpFileHub.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // TODO: Реализация методов репозитория
    }
}