using FinCore.BuildingBlocks.Responses;
using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Transaction.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/transactions")]
public class AdminTransactionsController : ControllerBase
{
    private readonly ITransactionQueryService _queryService;

    public AdminTransactionsController(ITransactionQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TransactionQuery query)
    {
        var result = await _queryService.GetAdminTransactionsAsync(query);
        return Ok(ApiResponse<PagedResult<AdminTransactionItem>>.Ok(result));
    }

    [HttpGet("suspicious")]
    public async Task<IActionResult> GetSuspicious([FromQuery] TransactionQuery query)
    {
        query.SuspiciousOnly = true;
        var result = await _queryService.GetAdminTransactionsAsync(query);
        return Ok(ApiResponse<PagedResult<AdminTransactionItem>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _queryService.GetStatsAsync();
        return Ok(ApiResponse<TransactionStatsResponse>.Ok(result));
    }
}