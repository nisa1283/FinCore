using FinCore.Account.Application.DTOs;
using FinCore.Account.Application.Services;
using FinCore.Account.Domain.Entities;
using FinCore.BuildingBlocks.Responses;
using FinCore.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Account.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request)
    {
        var result = await _accountService.CreateAsync(User.GetUserId(), request);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AccountResponse>.Ok(result, "Account created."));
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var result = await _accountService.GetMyAccountsAsync(User.GetUserId());
        return Ok(ApiResponse<List<AccountResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _accountService.GetByIdAsync(User.GetUserId(), User.IsInRole("Admin"), id);
        return Ok(ApiResponse<AccountResponse>.Ok(result));
    }

    [HttpPost("{id:guid}/freeze")]
    public async Task<IActionResult> Freeze(Guid id)
    {
        var result = await _accountService.SetStatusAsync(User.GetUserId(), User.IsInRole("Admin"), id, AccountStatus.Frozen);
        return Ok(ApiResponse<AccountResponse>.Ok(result, "Account frozen."));
    }

    [HttpPost("{id:guid}/unfreeze")]
    public async Task<IActionResult> Unfreeze(Guid id)
    {
        var result = await _accountService.SetStatusAsync(User.GetUserId(), User.IsInRole("Admin"), id, AccountStatus.Active);
        return Ok(ApiResponse<AccountResponse>.Ok(result, "Account unfrozen."));
    }
}