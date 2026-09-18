using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Discovery;
using Haggly.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence;

public static partial class ApplicationDataSeeder
{
    private static readonly DiscoveryProductSeed[] DiscoveryProductSeeds =
    [
        new("CI_BUN_SOI_TO", "Bún sợi to tươi", "Bún sợi lớn dùng cho bún bò Huế", ProductUnit.KG, "bun-mi-do-kho", "BT-001-S01", 1m, 34_000m, 55m),
        new("CI_BUN_SOI_TO", "Bún bò sợi lớn đặc biệt", "Bún tươi sợi lớn làm trong ngày", ProductUnit.KG, "bun-mi-do-kho", "BM-001-S05", 0.5m, 38_000m, 32m),
        new("CI_CHAN_GIO_HEO", "Chân giò heo", "Chân giò heo tươi, làm sạch", ProductUnit.KG, "thit-tuoi", "BR-001-S02", 0.5m, 118_000m, 38m),
        new("CI_CHAN_GIO_HEO", "Chân giò heo rút xương", "Chân giò sơ chế tiện lợi", ProductUnit.KG, "thit-tuoi", "TD-001-S03", 0.5m, 145_000m, 18m),
        new("CI_NAM_BO", "Nạm bò", "Nạm bò tươi phù hợp nấu bún bò và phở", ProductUnit.KG, "thit-tuoi", "BR-001-S02", 0.5m, 218_000m, 24m),
        new("CI_HUYET_BO_HOAC_HEO", "Huyết heo tươi", "Huyết heo đã sơ chế", ProductUnit.KG, "thit-tuoi", "BR-001-S02", 0.5m, 42_000m, 20m),
        new("CI_CHA_CUA_HAY_CHA_BO", "Chả cua Huế", "Chả cua dùng cho bún bò Huế", ProductUnit.KG, "hai-san", "TD-001-S03", 0.5m, 175_000m, 16m),
        new("CI_DAU_MAU_DIEU", "Dầu màu điều", "Dầu điều tạo màu tự nhiên", ProductUnit.LITER, "gia-vi", "BM-001-S05", 0.25m, 92_000m, 14m),
        new("CI_SA", "Sả cây", "Sả tươi thơm, bán theo bó", ProductUnit.BUNCH, "rau-cu", "BT-001-S01", 1m, 12_000m, 65m),
        new("CI_SA", "Sả bó loại 1", "Sả cây chọn lọc", ProductUnit.BUNCH, "rau-cu", "BM-001-S05", 1m, 15_000m, 30m),
        new("CI_HANH_TAY", "Hành tây", "Hành tây củ chắc, vị ngọt", ProductUnit.KG, "rau-cu", "BT-001-S01", 0.5m, 28_000m, 48m),
        new("CI_TOI", "Tỏi ta", "Tỏi ta thơm cay", ProductUnit.KG, "gia-vi", "BM-001-S05", 0.25m, 86_000m, 22m),
        new("CI_GUNG", "Gừng già", "Gừng già thơm, phù hợp nấu nước dùng", ProductUnit.KG, "gia-vi", "BM-001-S05", 0.25m, 48_000m, 25m),
        new("CI_MAM_RUOC", "Mắm ruốc Huế", "Mắm ruốc Huế dùng nêm bún bò", ProductUnit.KG, "gia-vi", "BM-001-S05", 0.25m, 95_000m, 18m),
        new("CI_HANH_LA", "Hành lá", "Hành lá tươi trong ngày", ProductUnit.BUNCH, "rau-cu", "BT-001-S01", 1m, 9_000m, 80m),
        new("CI_GIA_DO", "Giá đỗ", "Giá đỗ sạch, giòn ngọt", ProductUnit.KG, "rau-cu", "BT-001-S01", 0.5m, 24_000m, 42m),
        new("CI_MUI_TAU_HUNG_QUE", "Mùi tàu và húng quế", "Rau thơm ăn kèm bún bò", ProductUnit.BUNCH, "rau-cu", "BT-001-S01", 1m, 14_000m, 36m),
        new("CI_HOA_CHUOI", "Hoa chuối bào", "Hoa chuối bào sẵn ăn kèm", ProductUnit.KG, "rau-cu", "BT-001-S01", 0.5m, 36_000m, 28m),
        new("CI_CHANH", "Chanh không hạt", "Chanh tươi mọng nước", ProductUnit.KG, "trai-cay", "HT-001-S04", 0.5m, 32_000m, 40m),
        new("CI_NUOC_MAM", "Nước mắm nhĩ 40 độ đạm", "Nước mắm truyền thống Phú Quốc", ProductUnit.LITER, "gia-vi", "BM-001-S05", 1m, 145_000m, 36m)
    ];

    private static async Task EnsureDiscoveryMarketplaceAsync(
        HagglyDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var requiredCodes = DiscoveryProductSeeds
            .Select(seed => seed.CanonicalCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var canonicalIngredients = await dbContext.CanonicalIngredients
            .Where(ingredient => requiredCodes.Contains(ingredient.Code) && ingredient.IsActive)
            .ToDictionaryAsync(ingredient => ingredient.Code, StringComparer.Ordinal, cancellationToken);

        // The explicit importer owns Discovery knowledge data. API startup only
        // extends the development marketplace after that data is available.
        if (canonicalIngredients.Count != requiredCodes.Length)
        {
            return;
        }

        var actor = await dbContext.Users.IgnoreQueryFilters().SingleAsync(
            user => user.Email == "seed.platform.admin@haggly.develop",
            cancellationToken);
        var stalls = await dbContext.Stalls.IgnoreQueryFilters()
            .Where(stall => DiscoveryProductSeeds.Select(seed => seed.StallCode).Contains(stall.Code))
            .ToDictionaryAsync(stall => stall.Code, StringComparer.Ordinal, cancellationToken);
        var inventories = await dbContext.Inventories
            .Include(inventory => inventory.Items)
            .Where(inventory => stalls.Values.Select(stall => stall.Id).Contains(inventory.StallId))
            .ToDictionaryAsync(inventory => inventory.StallId, cancellationToken);

        var categories = await dbContext.Categories.IgnoreQueryFilters()
            .ToDictionaryAsync(category => category.Slug, StringComparer.Ordinal, cancellationToken);
        if (!categories.TryGetValue("bun-mi-do-kho", out var dryGoodsCategory))
        {
            dryGoodsCategory = new Category
            {
                Name = "Bún, mì và đồ khô",
                Slug = "bun-mi-do-kho",
                Description = "Bún, mì và nguyên liệu khô dùng hằng ngày",
                DisplayOrder = categories.Count + 1,
                Status = CatalogStatus.ACTIVE,
                CreatedAt = now,
                CreatedBy = actor.Id
            };
            categories.Add(dryGoodsCategory.Slug, dryGoodsCategory);
            dbContext.Categories.Add(dryGoodsCategory);
        }

        var seedNames = DiscoveryProductSeeds.Select(seed => seed.Name).Distinct().ToArray();
        var products = await dbContext.Products.IgnoreQueryFilters()
            .Where(product => seedNames.Contains(product.Name))
            .ToDictionaryAsync(product => product.Name, StringComparer.Ordinal, cancellationToken);
        var productIds = products.Values.Select(product => product.Id).ToArray();
        var mappings = await dbContext.ProductIngredientMappings
            .Where(mapping => productIds.Contains(mapping.ProductId))
            .ToDictionaryAsync(mapping => mapping.ProductId, cancellationToken);
        var stallIds = stalls.Values.Select(stall => stall.Id).ToArray();
        var listings = await dbContext.ProductStalls.IgnoreQueryFilters()
            .Where(listing => stallIds.Contains(listing.StallId))
            .ToDictionaryAsync(
                listing => (listing.StallId, listing.ProductId),
                cancellationToken);

        foreach (var seed in DiscoveryProductSeeds)
        {
            var category = categories[seed.CategorySlug];
            var stall = stalls[seed.StallCode];
            var inventory = inventories[stall.Id];

            if (!products.TryGetValue(seed.Name, out var product))
            {
                product = new Product
                {
                    CategoryId = category.Id,
                    Name = seed.Name,
                    Description = seed.Description,
                    DefaultUnit = seed.Unit,
                    Status = CatalogStatus.ACTIVE,
                    CreatedAt = now,
                    CreatedBy = actor.Id
                };
                products.Add(product.Name, product);
                dbContext.Products.Add(product);
            }

            if (!mappings.ContainsKey(product.Id))
            {
                var mapping = new ProductIngredientMapping(
                    product.Id,
                    canonicalIngredients[seed.CanonicalCode].Id,
                    ProductIngredientMappingStatus.APPROVED,
                    ProductIngredientMappingMethod.IMPORT,
                    now);
                mappings.Add(product.Id, mapping);
                dbContext.ProductIngredientMappings.Add(mapping);
            }

            if (!listings.TryGetValue((stall.Id, product.Id), out var listing))
            {
                listing = ProductStall.Create(
                    stall.Id,
                    product.Id,
                    product.Name,
                    seed.Unit,
                    seed.MinimumOrderQuantity,
                    seed.UnitPrice,
                    isNegotiable: false);
                listing.CreatedAt = now;
                listing.CreatedBy = stall.VendorId;
                listings.Add((stall.Id, product.Id), listing);
                dbContext.ProductStalls.Add(listing);
            }

            if (inventory.Items.All(item => item.ProductStallId != listing.Id))
            {
                var item = inventory.AddItem(
                    listing.Id,
                    seed.Quantity,
                    stall.VendorId,
                    now);
                dbContext.InventoryItems.Add(item);
            }
        }
    }

    private sealed record DiscoveryProductSeed(
        string CanonicalCode,
        string Name,
        string Description,
        ProductUnit Unit,
        string CategorySlug,
        string StallCode,
        decimal MinimumOrderQuantity,
        decimal UnitPrice,
        decimal Quantity);
}
