using FinCore.Auth.Application.Abstractions;
using FinCore.Auth.Application.DTOs;
using FinCore.Auth.Domain.Entities;
using FinCore.BuildingBlocks.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinCore.Auth.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IAuthDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;

    public AuthService(
        IAuthDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IValidator<UpdateProfileRequest> updateProfileValidator)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<UserProfileResponse> RegisterAsync(RegisterRequest request)
    {
        await _registerValidator.ValidateAndThrowAsync(request);

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new ConflictException("This email is already registered.");

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = _hasher.Hash(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ToProfile(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        await _loginValidator.ValidateAndThrowAsync(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // E-posta yoksa da şifre yanlışsa da aynı mesajı veriyoruz,
        // böylece saldırgan hangi e-postaların kayıtlı olduğunu öğrenemez.
        if (user is null)
            throw new UnauthorizedAppException(InvalidCredentialsMessage);

        if (!user.IsActive)
            throw new ForbiddenException("This account is disabled.");

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            throw new ForbiddenException($"Account is locked. Try again after {user.LockoutEnd:HH:mm} UTC.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync(); // sayacı kaydet, sonra hata fırlat
            throw new UnauthorizedAppException(InvalidCredentialsMessage);
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request)
    {
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        if (stored is null || !stored.IsActive || stored.User is null || !stored.User.IsActive)
            throw new UnauthorizedAppException("Invalid refresh token.");

        // Token rotation: eskisini iptal et, yenisini ver
        stored.RevokedAt = DateTime.UtcNow;

        return await IssueTokensAsync(stored.User);
    }

    public async Task LogoutAsync(Guid userId, RefreshTokenRequest request)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.UserId == userId);

        if (stored is not null && stored.IsActive)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        await _changePasswordValidator.ValidateAndThrowAsync(request);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleException("Current password is incorrect.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);

        // Güvenlik: şifre değişince tüm cihazlardaki oturumları kapat
        var activeTokens = await _db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
            token.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");

        return ToProfile(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        await _updateProfileValidator.ValidateAndThrowAsync(request);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");

        user.FullName = request.FullName.Trim();
        await _db.SaveChangesAsync();

        return ToProfile(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var (accessToken, expiresAt) = _tokenService.CreateAccessToken(user);
        var refreshToken = _tokenService.CreateRefreshToken(user.Id);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(); // kullanıcıdaki değişiklikler (sayaç sıfırlama vb.) de burada kaydolur

        return new AuthResponse(accessToken, refreshToken.Token, expiresAt);
    }

    private static UserProfileResponse ToProfile(User user) =>
        new(user.Id, user.Email, user.FullName, user.Role);
}