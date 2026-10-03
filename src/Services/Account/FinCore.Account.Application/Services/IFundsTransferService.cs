using FinCore.BuildingBlocks.Contracts;

namespace FinCore.Account.Application.Services;

public interface IFundsTransferService
{
    Task<InternalTransferResponse> TransferAsync(InternalTransferRequest request);
}