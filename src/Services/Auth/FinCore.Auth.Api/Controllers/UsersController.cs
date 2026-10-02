using FinCore.Auth.Application.DTOs;
using FinCore.Auth.Application.Services;
using FinCore.BuildingBlocks.Responses;
using FinCore.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Auth.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _authService.GetProfileAsync(User.GetUserId());
        return Ok(ApiResponse<UserProfileResponse>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var result = await _authService.UpdateProfileAsync(User.GetUserId(), request);
        return Ok(ApiResponse<UserProfileResponse>.Ok(result, "Profile updated."));
    }
}