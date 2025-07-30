using Microsoft.AspNetCore.Mvc;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Application.Services;

namespace CorpFileHub.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserContextService _userContext;

        public UsersController(IUserRepository userRepository, IUserContextService userContext)
        {
            _userRepository = userRepository;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = _userContext.GetCurrentUserId() ?? 0;
            if (userId == 0)
                return Unauthorized();
            var users = await _userRepository.GetAllAsync();
            var result = users.Select(u => new { u.Id, u.FullName, u.Email });
            return Ok(result);
        }
    }
}
