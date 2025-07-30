using Microsoft.AspNetCore.Http;
using System;

namespace CorpFileHub.Application.Services
{
    public interface IUserContextService
    {
        int? GetCurrentUserId();
    }

    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetCurrentUserId()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return null;
            if (session.TryGetValue("UserId", out var bytes) && bytes.Length >= 4)
            {
                return BitConverter.ToInt32(bytes, 0);
            }
            return null;
        }
    }
}
