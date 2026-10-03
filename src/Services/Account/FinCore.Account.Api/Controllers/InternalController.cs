using FinCore.Account.Application.Services;
using FinCore.BuildingBlocks.Contracts;
using FinCore.BuildingBlocks.Responses;
using FinCore.BuildingBlocks.Security;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Account.Api.Controllers;

[ApiController]
[InternalApiKey]
[Route("internal")]
public class InternalController : ControllerBase
{
    private readonly IFundsTransferService _transferService;

    public InternalController(IFundsTransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> Transfer(InternalTransferRequest request)
    {
        var result = await _transferService.TransferAsync(request);
        return Ok(ApiResponse<InternalTransferResponse>.Ok(result));
    }
}