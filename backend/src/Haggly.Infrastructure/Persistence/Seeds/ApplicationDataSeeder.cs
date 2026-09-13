using Haggly.Application.Abstractions.Identity;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Finance;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence;

public static class ApplicationDataSeeder
{
    private const string SeedMarkerEmail = "deliverer5@example.vn";
    private const string DevelopmentPassword = "Admin123!";
    private const long SeedLockId = 72_244_591;

    public static async Task SeedAsync(
        HagglyDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Prevent two application instances from racing through the marker check.
        await dbContext.Database.ExecuteSqlRawAsync(
            $"SELECT pg_advisory_xact_lock({SeedLockId})",
            cancellationToken);

        if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(
                user => user.Email == SeedMarkerEmail,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var now = new DateTimeOffset(2026, 9, 1, 1, 0, 0, TimeSpan.Zero);
        var roles = await EnsureRolesAsync(dbContext, now, cancellationToken);

        var admins = CreateAdmins(dbContext, passwordHasher, roles, now);
        var buyers = CreateBuyers(dbContext, passwordHasher, roles, now);
        var vendors = CreateVendors(dbContext, passwordHasher, roles, admins[0].Id, now);
        CreateDeliverers(dbContext, passwordHasher, roles, now);

        var markets = CreateMarkets(dbContext, admins[0].Id, now);
        var stalls = CreateStalls(dbContext, markets, vendors, now);
        var products = CreateCatalog(dbContext, admins[0].Id, now);
        var listings = CreateListings(dbContext, stalls, products, vendors, now);
        var inventoryItems = CreateInventories(dbContext, stalls, listings, vendors, now);
        CreateCarts(dbContext, buyers, inventoryItems, now);
        var orders = CreateOrders(dbContext, buyers, stalls, inventoryItems, products, now);
        var posSales = CreatePosSales(dbContext, stalls, vendors, inventoryItems, products, now);
        CreatePaymentsAndRevenue(dbContext, orders, posSales, now);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<RoleCode, Role>> EnsureRolesAsync(
        HagglyDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var seeds = new[]
        {
            (RoleCode.BUYER, "Buyer", "A market buyer."),
            (RoleCode.VENDOR, "Vendor", "A market vendor."),
            (RoleCode.MARKET_ADMIN, "Market administrator", "An administrator for market operations."),
            (RoleCode.PLATFORM_ADMIN, "Platform administrator", "A platform administrator."),
            (RoleCode.DELIVERER, "Deliverer", "A delivery operator.")
        };

        foreach (var seed in seeds.Where(seed => roles.All(role => role.Code != seed.Item1)))
        {
            var role = new Role
            {
                Code = seed.Item1,
                Name = seed.Item2,
                Description = seed.Item3,
                IsActive = true,
                CreatedAt = now
            };
            roles.Add(role);
            dbContext.Roles.Add(role);
        }

        return roles.ToDictionary(role => role.Code);
    }

    private static User[] CreateAdmins(
        HagglyDbContext dbContext,
        IPasswordHasher passwordHasher,
        IReadOnlyDictionary<RoleCode, Role> roles,
        DateTimeOffset now)
    {
        var seeds = new[]
        {
            ("seed.market.admin@haggly.develop", "Nguyễn Minh Anh", "0901000001", "MA-SEED-001", AdminScope.MARKET, RoleCode.MARKET_ADMIN),
            ("seed.platform.admin@haggly.develop", "Trần Quốc Bảo", "0901000002", "PA-SEED-001", AdminScope.PLATFORM, RoleCode.PLATFORM_ADMIN),
            ("operations.admin@haggly.develop", "Lê Thanh Hà", "0901000003", "MA-002", AdminScope.MARKET, RoleCode.MARKET_ADMIN),
            ("catalog.admin@haggly.develop", "Phạm Ngọc Linh", "0901000004", "MA-003", AdminScope.MARKET, RoleCode.MARKET_ADMIN),
            ("support.admin@haggly.develop", "Võ Hoàng Nam", "0901000005", "PA-002", AdminScope.PLATFORM, RoleCode.PLATFORM_ADMIN)
        };

        return seeds.Select((seed, index) =>
        {
            var user = CreateUser(seed.Item1, seed.Item2, seed.Item3, passwordHasher, now.AddMinutes(index));
            dbContext.Users.Add(user);
            dbContext.AdminProfiles.Add(new AdminProfile
            {
                UserId = user.Id,
                EmployeeCode = seed.Item4,
                AdminScope = seed.Item5,
                CreatedAt = now,
                CreatedBy = user.Id
            });
            AddRole(dbContext, user, roles[seed.Item6], now);
            return user;
        }).ToArray();
    }

    private static User[] CreateBuyers(
        HagglyDbContext dbContext,
        IPasswordHasher passwordHasher,
        IReadOnlyDictionary<RoleCode, Role> roles,
        DateTimeOffset now)
    {
        var seeds = new[]
        {
            ("lan.nguyen@example.vn", "Nguyễn Thị Lan", "0912345601", "Nhận hàng sau 17:30"),
            ("hung.tran@example.vn", "Trần Văn Hùng", "0912345602", "Gọi trước khi giao"),
            ("mai.le@example.vn", "Lê Ngọc Mai", "0912345603", "Nhận tại cổng chính"),
            ("tuan.pham@example.vn", "Phạm Anh Tuấn", "0912345604", "Không dùng túi nhựa"),
            ("thao.vo@example.vn", "Võ Thu Thảo", "0912345605", "Kiểm tra đơn cùng người bán")
        };

        return seeds.Select((seed, index) =>
        {
            var user = CreateUser(seed.Item1, seed.Item2, seed.Item3, passwordHasher, now.AddHours(-index));
            dbContext.Users.Add(user);
            dbContext.BuyerProfiles.Add(new BuyerProfile
            {
                UserId = user.Id,
                DefaultPickupNote = seed.Item4,
                CreatedAt = now,
                CreatedBy = user.Id
            });
            AddRole(dbContext, user, roles[RoleCode.BUYER], now);
            return user;
        }).ToArray();
    }

    private static User[] CreateVendors(
        HagglyDbContext dbContext,
        IPasswordHasher passwordHasher,
        IReadOnlyDictionary<RoleCode, Role> roles,
        Guid approvedBy,
        DateTimeOffset now)
    {
        var seeds = new[]
        {
            ("vendor.rau@example.vn", "Đỗ Văn Thành", "0922000001", "Nông Sản Xanh Thành Phát", "0318451001", "0318451001"),
            ("vendor.thit@example.vn", "Bùi Kim Oanh", "0922000002", "Thực Phẩm An Tâm", "0318451002", "0318451002"),
            ("vendor.haisan@example.vn", "Ngô Đức Hải", "0922000003", "Hải Sản Biển Đông", "0318451003", "0318451003"),
            ("vendor.traicay@example.vn", "Huỳnh Mỹ Duyên", "0922000004", "Trái Cây Miền Tây", "0318451004", "0318451004"),
            ("vendor.giavi@example.vn", "Phan Quốc Việt", "0922000005", "Gia Vị Việt", "0318451005", "0318451005")
        };

        return seeds.Select((seed, index) =>
        {
            var user = CreateUser(seed.Item1, seed.Item2, seed.Item3, passwordHasher, now.AddDays(-30 - index));
            dbContext.Users.Add(user);
            dbContext.VendorProfiles.Add(new VendorProfile
            {
                UserId = user.Id,
                BusinessName = seed.Item4,
                BusinessRegistrationNo = seed.Item5,
                TaxCode = seed.Item6,
                ApprovalStatus = ApprovalStatus.APPROVED,
                ApprovedAt = now.AddDays(-20),
                ApprovedBy = approvedBy,
                CreatedAt = now.AddDays(-30),
                CreatedBy = user.Id
            });
            AddRole(dbContext, user, roles[RoleCode.VENDOR], now);
            return user;
        }).ToArray();
    }

    private static void CreateDeliverers(
        HagglyDbContext dbContext,
        IPasswordHasher passwordHasher,
        IReadOnlyDictionary<RoleCode, Role> roles,
        DateTimeOffset now)
    {
        var names = new[] { "Nguyễn Công Danh", "Trần Gia Huy", "Lê Thành Đạt", "Phạm Minh Khoa", "Vũ Quang Vinh" };
        var plates = new[] { "59-A3 128.45", "59-B2 306.18", "50-N1 745.22", "51-L2 883.09", "59-C1 519.73" };
        for (var index = 0; index < names.Length; index++)
        {
            var email = index == names.Length - 1 ? SeedMarkerEmail : $"deliverer{index + 1}@example.vn";
            var user = CreateUser(email, names[index], $"093300000{index + 1}", passwordHasher, now);
            dbContext.Users.Add(user);
            dbContext.DelivererProfiles.Add(new DelivererProfile
            {
                UserId = user.Id,
                VehicleType = VehicleType.MOTORBIKE,
                VehiclePlate = plates[index],
                ApprovalStatus = ApprovalStatus.APPROVED,
                CreatedAt = now,
                CreatedBy = user.Id
            });
            AddRole(dbContext, user, roles[RoleCode.DELIVERER], now);
        }
    }

    private static Market[] CreateMarkets(HagglyDbContext dbContext, Guid actorId, DateTimeOffset now)
    {
        var seeds = new[]
        {
            ("BT-001", "Chợ Bến Thành", "Lê Lợi, Phường Bến Thành, Quận 1", 10.7725m, 106.6980m),
            ("BR-001", "Chợ Bà Rịa", "Nguyễn Hữu Thọ, Phường Phước Trung, Bà Rịa", 10.4963m, 107.1684m),
            ("TD-001", "Chợ Thủ Đức", "Võ Văn Ngân, Phường Trường Thọ, Thủ Đức", 10.8505m, 106.7585m),
            ("HT-001", "Chợ Hòa Hưng", "Trần Văn Đang, Phường 11, Quận 3", 10.7824m, 106.6778m),
            ("BM-001", "Chợ Bình Tây", "Tháp Mười, Phường 2, Quận 6", 10.7496m, 106.6503m)
        };
        var values = seeds.Select(seed => new Market
        {
            Code = seed.Item1,
            Name = seed.Item2,
            Address = seed.Item3,
            Latitude = seed.Item4,
            Longitude = seed.Item5,
            OpeningTime = new TimeOnly(5, 0),
            ClosingTime = new TimeOnly(19, 0),
            Status = MarketStatus.ACTIVE,
            CreatedAt = now,
            CreatedBy = actorId
        }).ToArray();
        dbContext.Markets.AddRange(values);
        return values;
    }

    private static Stall[] CreateStalls(HagglyDbContext dbContext, Market[] markets, User[] vendors, DateTimeOffset now)
    {
        var names = new[] { "Rau Củ Thành Phát", "Thịt Sạch An Tâm", "Hải Sản Biển Đông", "Trái Cây Cô Duyên", "Gia Vị Quê Nhà" };
        var values = Enumerable.Range(0, 5).Select(index => new Stall
        {
            MarketId = markets[index].Id,
            VendorId = vendors[index].Id,
            Code = $"{markets[index].Code}-S{index + 1:00}",
            Name = names[index],
            LocationDescription = $"Dãy {index + 1}, quầy {10 + index}",
            PhoneNumber = vendors[index].PhoneNumber,
            Status = StallStatus.ACTIVE,
            CreatedAt = now,
            CreatedBy = vendors[index].Id
        }).ToArray();
        dbContext.Stalls.AddRange(values);
        return values;
    }

    private static Product[] CreateCatalog(HagglyDbContext dbContext, Guid actorId, DateTimeOffset now)
    {
        var categorySeeds = new[]
        {
            ("Rau củ", "rau-cu", "Rau củ tươi trong ngày"),
            ("Thịt tươi", "thit-tuoi", "Thịt được kiểm soát nguồn gốc"),
            ("Hải sản", "hai-san", "Hải sản tươi và cấp đông"),
            ("Trái cây", "trai-cay", "Trái cây theo mùa"),
            ("Gia vị", "gia-vi", "Gia vị và nguyên liệu nấu ăn")
        };
        var categories = categorySeeds.Select((seed, index) => new Category
        {
            Name = seed.Item1,
            Slug = seed.Item2,
            Description = seed.Item3,
            DisplayOrder = index + 1,
            Status = CatalogStatus.ACTIVE,
            CreatedAt = now,
            CreatedBy = actorId
        }).ToArray();
        dbContext.Categories.AddRange(categories);

        var productSeeds = new[]
        {
            ("Cải thìa Đà Lạt", "Cải thìa giòn, thu hoạch trong ngày", ProductUnit.KG),
            ("Thịt ba rọi heo", "Ba rọi heo VietGAP, tỷ lệ nạc mỡ cân đối", ProductUnit.KG),
            ("Tôm sú sống", "Tôm sú cỡ 25–30 con/kg", ProductUnit.KG),
            ("Bưởi da xanh Bến Tre", "Bưởi loại 1, vị ngọt thanh", ProductUnit.PIECE),
            ("Nước mắm nhĩ 40 độ đạm", "Nước mắm truyền thống Phú Quốc", ProductUnit.LITER)
        };
        var products = productSeeds.Select((seed, index) => new Product
        {
            CategoryId = categories[index].Id,
            Name = seed.Item1,
            Description = seed.Item2,
            DefaultUnit = seed.Item3,
            Status = CatalogStatus.ACTIVE,
            CreatedAt = now,
            CreatedBy = actorId
        }).ToArray();
        dbContext.Products.AddRange(products);
        return products;
    }

    private static ProductStall[] CreateListings(
        HagglyDbContext dbContext, Stall[] stalls, Product[] products, User[] vendors, DateTimeOffset now)
    {
        var prices = new[] { 32_000m, 165_000m, 285_000m, 78_000m, 145_000m };
        var values = Enumerable.Range(0, 5).Select(index =>
        {
            var value = ProductStall.Create(stalls[index].Id, products[index].Id, products[index].Name,
                products[index].DefaultUnit, 1m, prices[index], index < 3);
            value.CreatedAt = now;
            value.CreatedBy = vendors[index].Id;
            return value;
        }).ToArray();
        dbContext.ProductStalls.AddRange(values);
        return values;
    }

    private static InventoryItem[] CreateInventories(
        HagglyDbContext dbContext, Stall[] stalls, ProductStall[] listings, User[] vendors, DateTimeOffset now)
    {
        var quantities = new[] { 85m, 42m, 28m, 64m, 36m };
        var items = new List<InventoryItem>();
        for (var index = 0; index < 5; index++)
        {
            var inventory = Inventory.Create(stalls[index].Id, vendors[index].Id, now.AddDays(-2));
            items.Add(inventory.AddItem(listings[index].Id, quantities[index], vendors[index].Id, now.AddDays(-1)));
            dbContext.Inventories.Add(inventory);
        }
        return items.ToArray();
    }

    private static void CreateCarts(HagglyDbContext dbContext, User[] buyers, InventoryItem[] items, DateTimeOffset now)
    {
        for (var index = 0; index < 5; index++)
        {
            var cart = Cart.Create(buyers[index].Id, now.AddHours(-index));
            cart.AddItem(items[index].Id, index == 3 ? 2m : 1m, index == 1 ? "Cắt miếng vừa ăn" : null, now);
            dbContext.Carts.Add(cart);
        }
    }

    private static Order[] CreateOrders(
        HagglyDbContext dbContext, User[] buyers, Stall[] stalls, InventoryItem[] items, Product[] products, DateTimeOffset now)
    {
        var prices = new[] { 32_000m, 165_000m, 285_000m, 78_000m, 145_000m };
        var values = Enumerable.Range(0, 5).Select(index =>
        {
            var order = Order.Place(Guid.NewGuid(), buyers[index].Id,
                [new OrderItemInput(items[index].Id, stalls[index].Id, products[index].Name,
                    products[index].DefaultUnit, prices[index], index == 3 ? 2m : 1m, null)],
                now.AddDays(-10 + index));
            order.Status = OrderStatus.AGREED;
            foreach (var fulfillment in order.StallFulfillments)
                fulfillment.Status = StallFulfillmentStatus.AGREED;
            return order;
        }).ToArray();
        dbContext.Orders.AddRange(values);
        return values;
    }

    private static PosSale[] CreatePosSales(
        HagglyDbContext dbContext, Stall[] stalls, User[] vendors, InventoryItem[] items, Product[] products, DateTimeOffset now)
    {
        var prices = new[] { 32_000m, 165_000m, 285_000m, 78_000m, 145_000m };
        var values = Enumerable.Range(0, 5).Select(index => PosSale.Complete(
            Guid.NewGuid(), stalls[index].Id, vendors[index].Id, $"seed-pos-{index + 1}",
            [new PosSaleItemInput(items[index].Id, products[index].Name, products[index].DefaultUnit, prices[index], 1m)],
            now.AddDays(-5 + index), index % 2 == 0 ? PaymentMethodCode.CASH : PaymentMethodCode.BANK_TRANSFER)).ToArray();
        dbContext.PosSales.AddRange(values);
        return values;
    }

    private static void CreatePaymentsAndRevenue(
        HagglyDbContext dbContext, Order[] orders, PosSale[] posSales, DateTimeOffset now)
    {
        for (var index = 0; index < 5; index++)
        {
            var fulfillment = orders[index].StallFulfillments.Single();
            var payment = Payment.Create(Guid.NewGuid(), orders[index].Id, orders[index].TotalToCharge, "VND", now.AddDays(-4 + index));
            payment.StartProcessing(now.AddDays(-4 + index).AddMinutes(1));
            payment.MarkPaid(now.AddDays(-4 + index).AddMinutes(2));
            var paymentTransaction = PaymentTransaction.Create(Guid.NewGuid(), payment, payment.AmountDue, payment.InitiatedAt);
            paymentTransaction.MarkSucceeded($"HAGGLY-SEED-{index + 1:000}", "00", "Seeded successful payment", payment.CompletedAt!.Value);
            var allocation = PaymentAllocation.CreateSale(Guid.NewGuid(), paymentTransaction.Id, fulfillment.Id,
                fulfillment.StallId, fulfillment.FinalAmount, payment.CompletedAt.Value);
            payment.Transactions.Add(paymentTransaction);
            paymentTransaction.Allocations.Add(allocation);
            dbContext.Payments.Add(payment);

            dbContext.RevenueLedgers.Add(index < 3
                ? RevenueLedger.CreatePosSaleEntry(posSales[index].Id, posSales[index].StallId,
                    posSales[index].TotalAmount, posSales[index].CompletedAt)
                : RevenueLedger.CreatePaymentSaleEntry(allocation.Id, fulfillment.Id, fulfillment.StallId,
                    fulfillment.FinalAmount, payment.CompletedAt.Value));
        }
    }

    private static User CreateUser(
        string email, string fullName, string phoneNumber, IPasswordHasher passwordHasher, DateTimeOffset createdAt)
    {
        var user = new User
        {
            Email = email,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            Status = UserStatus.ACTIVE,
            EmailVerifiedAt = createdAt,
            PhoneVerifiedAt = createdAt,
            CreatedAt = createdAt
        };
        user.PasswordHash = passwordHasher.Hash(user, DevelopmentPassword);
        return user;
    }

    private static void AddRole(HagglyDbContext dbContext, User user, Role role, DateTimeOffset now)
        => dbContext.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAt = now,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = user.Id
        });
}
