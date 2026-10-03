namespace FinCore.Account.Domain.Entities;

public enum AccountStatus
{
    Active = 1,
    Frozen = 2
}

public class BankAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }                       // Auth Service'teki kullanıcının Id'si 
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;       // "Maaş hesabı" gibi
    public string Currency { get; set; } = "TRY";
    public decimal Balance { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Aynı anda iki işlem aynı hesabı değiştirmeye çalışırsa SQL Server bunu yakalar (concurrency)
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}