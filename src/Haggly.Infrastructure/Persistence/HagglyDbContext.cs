using Microsoft.EntityFrameworkCore;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Infrastructure.Persistence;

public sealed class HagglyDbContext(DbContextOptions<HagglyDbContext> options) : DbContext(options)
{
    //Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<BuyerProfile> BuyerProfiles => Set<BuyerProfile>();
    public DbSet<VendorProfile> VendorProfiles => Set<VendorProfile>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();
    public DbSet<DelivererProfile> DelivererProfiles => Set<DelivererProfile>();
   
    //Markets
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Stall> Stalls => Set<Stall>();
    
    // Catalog
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductStall> ProductStalls => Set<ProductStall>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HagglyDbContext).Assembly);
    }
}
