using FinCore.Account.Application.DTOs;
using FinCore.Account.Application.Services;
using FinCore.BuildingBlocks.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Account.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/accounts")]
public class AdminAccountsController : ControllerBase
{
    private readonly IAdminAccountService _adminService;

    public AdminAccountsController(IAdminAccountService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AdminAccountQuery query)
    {
        var result = await _adminService.GetAccountsAsync(query);
        return Ok(ApiResponse<PagedResult<AdminAccountItem>>.Ok(result));
    }
}