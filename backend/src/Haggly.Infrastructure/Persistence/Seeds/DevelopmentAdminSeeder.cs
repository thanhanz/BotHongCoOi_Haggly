using Haggly.Application.Abstractions.Identity;
using Haggly.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence;

public static class DevelopmentAdminSeeder
{
    public static async Task SeedAsync(
        HagglyDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var accounts = new[]
        {
            new AdminSeed(
                "market.admin@haggly.develop",
                "Market Admin",
                "market-admin",
                "0900000001",
                AdminScope.MARKET,
                RoleCode.MARKET_ADMIN,
                "Admin123!"),
            new AdminSeed(
                "platform.admin@haggly.develop",
                "Platform Admin",
                "platform-admin",
                "0900000002",
                AdminScope.PLATFORM,
                RoleCode.PLATFORM_ADMIN,
                "Admin123!")
        };

        foreach (var account in accounts)
        {
            var user = await dbContext.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(candidate => candidate.Email == account.Email, cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Email = account.Email,
                    PhoneNumber = account.PhoneNumber,
                    FullName = account.FullName,
                    Status = UserStatus.ACTIVE
                };
                user.PasswordHash = passwordHasher.Hash(user, account.Password);
                dbContext.Users.Add(user);
            }

            var profile = await dbContext.AdminProfiles
                .SingleOrDefaultAsync(candidate => candidate.UserId == user.Id, cancellationToken);
            if (profile is null)
            {
                dbContext.AdminProfiles.Add(new AdminProfile
                {
                    UserId = user.Id,
                    EmployeeCode = account.EmployeeCode,
                    AdminScope = account.Scope
                });
            }

            var role = await dbContext.Roles
                .SingleAsync(candidate => candidate.Code == account.Role, cancellationToken);
            var hasRole = await dbContext.UserRoles.AnyAsync(
                userRole => userRole.UserId == user.Id && userRole.RoleId == role.Id,
                cancellationToken);
            if (!hasRole)
            {
                dbContext.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTimeOffset.UtcNow,
                    IsActive = true
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record AdminSeed(
        string Email,
        string FullName,
        string EmployeeCode,
        string PhoneNumber,
        AdminScope Scope,
        RoleCode Role,
        string Password);
}
