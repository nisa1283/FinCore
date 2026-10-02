using FinCore.Auth.Domain.Entities;
using System;

namespace FinCore.Auth.Application.Abstractions;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
    RefreshToken CreateRefreshToken(Guid userId);
}