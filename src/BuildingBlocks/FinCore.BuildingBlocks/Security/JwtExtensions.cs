using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FinCore.BuildingBlocks.Security;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        var settings = section.Get<JwtSettings>()
            ?? throw new InvalidOperationException("'Jwt' ayarları bulunamadı.");

        if (string.IsNullOrWhiteSpace(settings.SecretKey) || settings.SecretKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SecretKey en az 32 karakter olmalı. 'dotnet user-secrets' ile ayarla.");

        services.Configure<JwtSettings>(section);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // "sub", "role" gibi isimler olduğu gibi kalsın
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization();
        return services;
    }
}