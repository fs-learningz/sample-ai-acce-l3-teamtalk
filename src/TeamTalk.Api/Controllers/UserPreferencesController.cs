using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Users;
using TeamTalk.Application.Users.Contracts;

namespace TeamTalk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me/theme")]
public class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly ICurrentUserService _currentUserService;

    public UserPreferencesController(
        IUserPreferencesService userPreferencesService,
        ICurrentUserService currentUserService)
    {
        _userPreferencesService = userPreferencesService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetThemeAsync()
    {
        var result = await _userPreferencesService.GetThemeAsync(_currentUserService.UserId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateThemeAsync(UpdateUserThemeRequest request)
    {
        var result = await _userPreferencesService.UpdateThemeAsync(_currentUserService.UserId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }
}
