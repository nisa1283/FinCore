using FinCore.BuildingBlocks.Contracts;

namespace FinCore.Transaction.Application.Abstractions;

public interface IAccountClient
{
    Task<InternalTransferResponse> TransferAsync(InternalTransferRequest request, CancellationToken cancellationToken = default);
}