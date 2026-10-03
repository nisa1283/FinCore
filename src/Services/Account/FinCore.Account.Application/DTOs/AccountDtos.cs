namespace FinCore.Account.Application.DTOs;

public record CreateAccountRequest(string Name, string Currency);

public record AccountResponse(
    Guid Id,
    string AccountNumber,
    string Name,
    string Currency,
    decimal Balance,
    string Status,
    DateTime CreatedAt);