using FinCore.Auth.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace FinCore.Auth.Application.Services;

public interface IAuthService
{
    Task<UserProfileResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request);
    Task LogoutAsync(Guid userId, RefreshTokenRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<UserProfileResponse> GetProfileAsync(Guid userId);
    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
}