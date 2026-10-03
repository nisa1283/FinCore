using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinCore.BuildingBlocks.Contracts;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.BuildingBlocks.Responses;
using FinCore.Transaction.Application.Abstractions;

namespace FinCore.Transaction.Infrastructure.Clients;

public class AccountClient : IAccountClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public AccountClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<InternalTransferResponse> TransferAsync(
        InternalTransferRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;

        try
        {
            response = await _http.PostAsJsonAsync("internal/transfers", request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("Account service is unavailable.");
        }
        catch (TaskCanceledException)
        {
            throw new ServiceUnavailableException("Account service timed out.");
        }

        ApiResponse<InternalTransferResponse>? body = null;
        try
        {
            body = await response.Content.ReadFromJsonAsync<ApiResponse<InternalTransferResponse>>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {}

        if (response.IsSuccessStatusCode && body?.Data is not null)
            return body.Data;

        var message = body?.Message ?? "Transfer failed.";

        throw response.StatusCode switch
        {
            HttpStatusCode.NotFound => new NotFoundException(message),
            HttpStatusCode.Forbidden => new ForbiddenException(message),
            HttpStatusCode.Conflict => new ConflictException(message),
            HttpStatusCode.BadRequest => new BusinessRuleException(message),
            _ => new ServiceUnavailableException("Account service returned an unexpected error.")
        };
    }
}