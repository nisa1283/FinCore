using System;

namespace FinCore.Auth.Application.DTOs;

public record RegisterRequest(string Email, string Password, string FullName);

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateProfileRequest(string FullName);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);

public record UserProfileResponse(Guid Id, string Email, string FullName, string Role);