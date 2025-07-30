using CorpFileHub.Application.DTOs;
using CorpFileHub.Application.UseCases.Audit;
using CorpFileHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CorpFileHub.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly GetAuditLogUseCase _getAuditLogUseCase;
        private readonly IUserContextService _userContext;
        private readonly ILogger<AuditController> _logger;

        public AuditController(GetAuditLogUseCase getAuditLogUseCase, IUserContextService userContext, ILogger<AuditController> logger)
        {
            _getAuditLogUseCase = getAuditLogUseCase;
            _userContext = userContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLog([FromQuery] AuditLogSearchDto search)
        {
            var userId = _userContext.GetCurrentUserId() ?? 0;
            if (userId == 0)
                return Unauthorized();

            try
            {
                var result = await _getAuditLogUseCase.GetAuditLogAsync(search, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения журнала аудита");
                return StatusCode(500, new { error = "Ошибка сервера" });
            }
        }
    }
}

