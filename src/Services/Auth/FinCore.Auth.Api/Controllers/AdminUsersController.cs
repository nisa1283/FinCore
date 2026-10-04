using FinCore.Auth.Application.DTOs;
using FinCore.Auth.Application.Services;
using FinCore.BuildingBlocks.Responses;
using FinCore.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Auth.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminService;

    public AdminUsersController(IAdminUserService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AdminUserQuery query)
    {
        var result = await _adminService.GetUsersAsync(query);
        return Ok(ApiResponse<PagedResult<AdminUserItem>>.Ok(result));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _adminService.SetActiveAsync(User.GetUserId(), id, true);
        return Ok(ApiResponse<AdminUserItem>.Ok(result, "User activated."));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _adminService.SetActiveAsync(User.GetUserId(), id, false);
        return Ok(ApiResponse<AdminUserItem>.Ok(result, "User deactivated."));
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var result = await _adminService.UnlockAsync(id);
        return Ok(ApiResponse<AdminUserItem>.Ok(result, "User unlocked."));
    }
}