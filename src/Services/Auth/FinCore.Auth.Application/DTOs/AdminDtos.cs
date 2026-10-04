namespace FinCore.Auth.Application.DTOs;

public class AdminUserQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }    // e-posta veya isim
    public bool? IsActive { get; set; }
}

public record AdminUserItem(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    bool IsLocked,
    DateTime CreatedAt);