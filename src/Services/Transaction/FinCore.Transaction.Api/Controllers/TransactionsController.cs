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
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        TransferRequest request)
    {
        var result = await _transactionService.TransferAsync(User.GetUserId(), idempotencyKey ?? string.Empty, request);

        var message = result.IsDuplicate
            ? "Duplicate request: the original result is returned."
            : "Transfer processed.";

        return Ok(ApiResponse<TransferResponse>.Ok(result, message));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _transactionService.GetByIdAsync(User.GetUserId(), id);
        return Ok(ApiResponse<TransferResponse>.Ok(result));
    }
}