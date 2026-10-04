using FinCore.BuildingBlocks.Responses;
using FinCore.BuildingBlocks.Security;
using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Transaction.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionHistoryController : ControllerBase
{
    private readonly ITransactionQueryService _queryService;

    public TransactionHistoryController(ITransactionQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] TransactionQuery query)
    {
        var result = await _queryService.GetMyTransactionsAsync(User.GetUserId(), query);
        return Ok(ApiResponse<PagedResult<TransactionHistoryItem>>.Ok(result));
    }
}