using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Application.Utilities;
using Microsoft.Extensions.Logging;

namespace CorpFileHub.Application.Services
{
    public interface IAuthenticationService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<string> HashPasswordAsync(string password);
        bool IsPasswordComplex(string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditService _auditService;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(IUserRepository userRepository, IAuditService auditService, ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Попытка входа для несуществующего или заблокированного пользователя: {Email}", email);
                await _auditService.LogLoginAttemptAsync(null, true, false, "unknown user or blocked");
                return null;
            }

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Неверный пароль для пользователя {Email}", email);
                await _auditService.LogLoginAttemptAsync(user.Id, true, false, "invalid password");
                return null;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _auditService.LogLoginAttemptAsync(user.Id, true, true);
            return user;
        }

        public Task<string> HashPasswordAsync(string password)
        {
            return Task.FromResult(PasswordHasher.HashPassword(password));
        }

        public bool IsPasswordComplex(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            return true;
        }
    }
}
