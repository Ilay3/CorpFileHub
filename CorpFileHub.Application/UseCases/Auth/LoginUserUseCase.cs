using CorpFileHub.Application.DTOs;
using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Application.UseCases.Auth
{
    public class LoginUserUseCase
    {
        private readonly IAuthenticationService _authService;
        private readonly IAuditService _auditService;

        public LoginUserUseCase(IAuthenticationService authService, IAuditService auditService)
        {
            _authService = authService;
            _auditService = auditService;
        }

        public async Task<UserProfileDto?> ExecuteAsync(UserLoginDto loginDto)
        {
            var user = await _authService.AuthenticateAsync(loginDto.Email, loginDto.Password);
            if (user == null)
            {
                return null;
            }

            await _auditService.LogSuccessAsync(user.Id, AuditAction.Login, "User", user.Id, user.Email);

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Department = user.Department,
                Position = user.Position,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
