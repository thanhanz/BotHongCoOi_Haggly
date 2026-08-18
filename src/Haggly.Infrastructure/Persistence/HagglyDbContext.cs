using Microsoft.EntityFrameworkCore;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using Haggly.Domain.Modules.Sales;
using Haggly.Domain.Modules.Finance;

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

    // Inventory
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryLedger> InventoryLedgers => Set<InventoryLedger>();

    // Sales
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<StallFulfillment> StallFulfillments => Set<StallFulfillment>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PosSale> PosSales => Set<PosSale>();
    public DbSet<PosSaleItem> PosSaleItems => Set<PosSaleItem>();

    // Finance
    public DbSet<RevenueLedger> RevenueLedgers => Set<RevenueLedger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HagglyDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareInventoryLedgerEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareInventoryLedgerEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareInventoryLedgerEntries()
    {
        ChangeTracker.DetectChanges();

        // Ledgers are append-only. EF can classify a new ledger added to an
        // already tracked listing as Modified because its Guid key is assigned
        // by the domain. Normalize that graph change to an INSERT.
        foreach (var entry in ChangeTracker.Entries<InventoryLedger>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
