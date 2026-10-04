using FinCore.Auth.Application.DTOs;
using FinCore.BuildingBlocks.Responses;

namespace FinCore.Auth.Application.Services;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserItem>> GetUsersAsync(AdminUserQuery query);
    Task<AdminUserItem> SetActiveAsync(Guid adminId, Guid userId, bool isActive);
    Task<AdminUserItem> UnlockAsync(Guid userId);
}