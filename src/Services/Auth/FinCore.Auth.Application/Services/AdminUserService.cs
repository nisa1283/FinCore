using FinCore.Auth.Application.Abstractions;
using FinCore.Auth.Application.DTOs;
using FinCore.Auth.Domain.Entities;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.BuildingBlocks.Responses;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Auth.Application.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IAuthDbContext _db;

    public AdminUserService(IAuthDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AdminUserItem>> GetUsersAsync(AdminUserQuery q)
    {
        var (page, pageSize) = Paging.Normalize(q.Page, q.PageSize);

        var query = _db.Users.AsNoTracking();

        if (q.IsActive.HasValue)
            query = query.Where(u => u.IsActive == q.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            query = query.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AdminUserItem>
        {
            Items = users.Select(ToItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AdminUserItem> SetActiveAsync(Guid adminId, Guid userId, bool isActive)
    {
        if (adminId == userId && !isActive)
            throw new BusinessRuleException("You cannot deactivate your own account.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");

        user.IsActive = isActive;

        if (!isActive)
        {
            // Pasif yapılan kullanıcının oturumlarını kapat
            var tokens = await _db.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAt == null)
                .ToListAsync();

            foreach (var token in tokens)
                token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return ToItem(user);
    }

    public async Task<AdminUserItem> UnlockAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");

        user.LockoutEnd = null;
        user.FailedLoginCount = 0;
        await _db.SaveChangesAsync();

        return ToItem(user);
    }

    private static AdminUserItem ToItem(User u) => new(
        u.Id, u.Email, u.FullName, u.Role, u.IsActive,
        u.LockoutEnd.HasValue && u.LockoutEnd > DateTime.UtcNow,
        u.CreatedAt);
}