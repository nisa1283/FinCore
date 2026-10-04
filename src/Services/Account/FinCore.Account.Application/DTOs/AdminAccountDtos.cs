namespace FinCore.Account.Application.DTOs;

public class AdminAccountQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? UserId { get; set; }
    public string? Status { get; set; }   // Active, Frozen
    public string? Search { get; set; }   // hesap numarası veya hesap adı
}

public record AdminAccountItem(
    Guid Id,
    Guid UserId,
    string AccountNumber,
    string Name,
    string Currency,
    decimal Balance,
    string Status,
    DateTime CreatedAt);