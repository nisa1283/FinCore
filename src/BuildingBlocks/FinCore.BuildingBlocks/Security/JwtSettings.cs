namespace FinCore.BuildingBlocks.Security;

public class JwtSettings
{
    public string Issuer { get; set; } = "FinCore";
    public string Audience { get; set; } = "FinCore.Clients";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}