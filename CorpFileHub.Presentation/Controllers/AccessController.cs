using Microsoft.AspNetCore.Mvc;
using CorpFileHub.Application.UseCases.Access;
using CorpFileHub.Application.Services;
using Microsoft.Extensions.Logging;

namespace CorpFileHub.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : ControllerBase
    {
        private readonly SetAccessRightsUseCase _accessUseCase;
        private readonly IUserContextService _userContext;
        private readonly ILogger<AccessController> _logger;

        public AccessController(SetAccessRightsUseCase accessUseCase, IUserContextService userContext, ILogger<AccessController> logger)
        {
            _accessUseCase = accessUseCase;
            _userContext = userContext;
            _logger = logger;
        }

        [HttpGet("files/{fileId}")]
        public async Task<IActionResult> GetFileAccess(int fileId)
        {
            var userId = _userContext.GetCurrentUserId() ?? 0;
            if (userId == 0)
                return Unauthorized();
            try
            {
                var result = await _accessUseCase.GetFileAccessListAsync(fileId, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения прав файла {FileId}", fileId);
                return StatusCode(500, new { error = "Ошибка сервера" });
            }
        }

        [HttpPost("files/{fileId}")]
        public async Task<IActionResult> SetFileAccess(int fileId, [FromBody] SetFileAccessRequest request)
        {
            var userId = _userContext.GetCurrentUserId() ?? 0;
            if (userId == 0)
                return Unauthorized();
            request.FileId = fileId;
            request.GrantedBy = userId;
            var result = await _accessUseCase.SetFileAccessAsync(request);
            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });
            return Ok(result);
        }

        [HttpGet("folders/{folderId}")]
        public async Task<IActionResult> GetFolderAccess(int folderId)
        {
            var userId = _userContext.GetCurrentUserId() ?? 0;
            if (userId == 0)
                return Unauthorized();
            try
            {
                var rules = await _accessUseCase.GetFolderAccessListAsync(folderId, userId);
                return Ok(rules);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения прав папки {FolderId}", folderId);
                return StatusCode(500, new { error = "Ошибка сервера" });
            }
        }

        [HttpPost("folders/{folderId}")]
        public async Task<IActionResult> SetFolderAccess(int folderId, [FromBody] SetFolderAccessRequest request)
        {
            var userId = _userContext.GetCurrentUserId() ?? 0;
            if (userId == 0)
                return Unauthorized();
            request.FolderId = folderId;
            request.GrantedBy = userId;
            var result = await _accessUseCase.SetFolderAccessAsync(request);
            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });
            return Ok(result);
        }
    }
}
