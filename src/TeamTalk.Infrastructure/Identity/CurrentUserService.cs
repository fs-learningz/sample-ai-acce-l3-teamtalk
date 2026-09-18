using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TeamTalk.Application.Common.Interfaces;

namespace TeamTalk.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim is null)
            {
                return null;
            }

            if (Guid.TryParse(claim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
    }

    public string? Email
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
