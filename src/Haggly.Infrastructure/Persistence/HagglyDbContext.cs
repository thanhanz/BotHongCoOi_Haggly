using Microsoft.EntityFrameworkCore;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Infrastructure.Persistence;

public sealed class HagglyDbContext(DbContextOptions<HagglyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<BuyerProfile> BuyerProfiles => Set<BuyerProfile>();
    public DbSet<VendorProfile> VendorProfiles => Set<VendorProfile>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();
    public DbSet<DelivererProfile> DelivererProfiles => Set<DelivererProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyIdentityConfiguration();
    }
}
