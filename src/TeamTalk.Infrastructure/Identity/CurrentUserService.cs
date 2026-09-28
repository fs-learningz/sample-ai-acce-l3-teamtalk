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

    public Guid UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim is not null && Guid.TryParse(claim.Value, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException("User is not authenticated.");
        }
    }

    public string Email
    {
        get
        {
            var email = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

            if (email is null)
            {
                throw new InvalidOperationException("User is not authenticated.");
            }

            return email;
        }
    }
}
