using Microsoft.AspNetCore.Mvc;
using TeamTalk.Application.Common.Interfaces;

namespace TeamTalk.Api.Common;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    protected ApiControllerBase(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected Guid CurrentUserId
    {
        get
        {
            var userId = _currentUserService.UserId;
            if (userId is null)
            {
                throw new InvalidOperationException("User is not authenticated.");
            }

            return userId.Value;
        }
    }
}
