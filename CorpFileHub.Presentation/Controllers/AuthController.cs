using CorpFileHub.Application.DTOs;
using CorpFileHub.Application.UseCases.Auth;
using CorpFileHub.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CorpFileHub.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoginUserUseCase _loginUserUseCase;
        private readonly IAuditService _auditService;

        public AuthController(LoginUserUseCase loginUserUseCase, IAuditService auditService)
        {
            _loginUserUseCase = loginUserUseCase;
            _auditService = auditService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            var profile = await _loginUserUseCase.ExecuteAsync(dto);
            if (profile == null)
            {
                await _auditService.LogLoginAttemptAsync(null, true, false, "invalid credentials");
                return Unauthorized();
            }

            HttpContext.Session.SetInt32("UserId", profile.Id);
            HttpContext.Session.SetString("FullName", profile.FullName);
            return Ok(profile);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            HttpContext.Session.Clear();
            if (userId.HasValue)
            {
                await _auditService.LogLoginAttemptAsync(userId.Value, false, true);
            }
            return Ok();
        }
    }
}
