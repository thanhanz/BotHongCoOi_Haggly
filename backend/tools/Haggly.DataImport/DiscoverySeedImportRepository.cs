using System.Text.Json;
using System.Text.Json.Serialization;
using Haggly.Domain.Modules.Discovery;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Haggly.DataImport;

public sealed record DishSeed([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name_vi")] string Name, [property: JsonPropertyName("name_normalized")] string NormalizedName, [property: JsonPropertyName("category")] string? Category, [property: JsonPropertyName("ingredients")] IReadOnlyList<SourceIngredientSeed> Ingredients);
public sealed record SourceIngredientSeed([property: JsonPropertyName("ingredient_id")] string Id, [property: JsonPropertyName("name_vi")] string Name, [property: JsonPropertyName("name_normalized")] string NormalizedName, [property: JsonPropertyName("category")] string? Category);
public sealed record CanonicalIngredientSeed([property: JsonPropertyName("code")] string Code, [property: JsonPropertyName("name_vi")] string Name, [property: JsonPropertyName("name_normalized")] string NormalizedName, [property: JsonPropertyName("category")] string? Category);
public sealed record SourceMappingSeed([property: JsonPropertyName("ingredient_id")] string IngredientId, [property: JsonPropertyName("canonical_code")] string CanonicalCode);
public sealed record ValidatedDiscoverySeed(IReadOnlyList<DishSeed> Dishes, IReadOnlyList<CanonicalIngredientSeed> Ingredients, IReadOnlyList<SourceMappingSeed> Mappings);
public sealed record ImportSummary(int Inserted, int Updated, int Unchanged, int CreatedRelations, int RemovedRelations, int Rejected);

public sealed class DiscoverySeedImportRepository(HagglyDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<ValidatedDiscoverySeed> ValidateAsync(string seedDirectory, CancellationToken ct)
    {
        var dishes = await ReadAsync<List<DishSeed>>(Path.Combine(seedDirectory, "common_dishes_seed.json"), ct);
        var ingredients = await ReadAsync<List<CanonicalIngredientSeed>>(Path.Combine(seedDirectory, "canonical_ingredients_seed.json"), ct);
        var mappings = await ReadAsync<List<SourceMappingSeed>>(Path.Combine(seedDirectory, "common_dish_ingredient_mappings.json"), ct);
        var errors = new List<string>();
        if (dishes.Count != 50) errors.Add($"Expected 50 dishes but found {dishes.Count}.");
        CheckUnique(dishes.Select(x => x.Id), "dish external ID", errors); CheckUnique(dishes.Select(x => x.NormalizedName), "dish normalized name", errors);
        CheckUnique(ingredients.Select(x => x.Code), "canonical code", errors); CheckUnique(ingredients.Select(x => x.NormalizedName), "canonical normalized name", errors);
        CheckUnique(mappings.Select(x => x.IngredientId), "source ingredient mapping", errors);
        foreach (var dish in dishes)
        {
            if (string.IsNullOrWhiteSpace(dish.Id) || string.IsNullOrWhiteSpace(dish.Name) || dish.Name.Length > 200 || dish.Id.Length > 64) errors.Add($"Dish '{dish.Id}' has invalid identity or name.");
            if (VietnameseNameNormalizer.Normalize(dish.Name) != dish.NormalizedName) errors.Add($"Dish '{dish.Id}' has a stale normalized name.");
            if (dish.Ingredients.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != dish.Ingredients.Count) errors.Add($"Dish '{dish.Id}' repeats an ingredient ID.");
            foreach (var source in dish.Ingredients)
            {
                if (string.IsNullOrWhiteSpace(source.Id) || string.IsNullOrWhiteSpace(source.Name) || source.Name.Length > 200) errors.Add($"Dish '{dish.Id}' contains an invalid source ingredient.");
                if (VietnameseNameNormalizer.Normalize(source.Name) != source.NormalizedName) errors.Add($"Source ingredient '{source.Id}' has a stale normalized name.");
            }
        }
        foreach (var ingredient in ingredients)
        {
            try { _ = CanonicalIngredient.Create(Guid.NewGuid(), ingredient.Code, ingredient.Name, ingredient.Category); } catch (Exception exception) { errors.Add($"Canonical ingredient '{ingredient.Code}' is invalid: {exception.Message}"); }
            if (VietnameseNameNormalizer.Normalize(ingredient.Name) != ingredient.NormalizedName) errors.Add($"Canonical ingredient '{ingredient.Code}' has a stale normalized name.");
        }
        var sourceIds = dishes.SelectMany(x => x.Ingredients).Select(x => x.Id).Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var mappedIds = mappings.Select(x => x.IngredientId).ToHashSet(StringComparer.Ordinal);
        if (!sourceIds.SetEquals(mappedIds)) errors.Add($"Mapping coverage differs from the {sourceIds.Count} source ingredient IDs.");
        var codes = ingredients.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var mapping in mappings.Where(x => !codes.Contains(x.CanonicalCode))) errors.Add($"Mapping '{mapping.IngredientId}' references unknown code '{mapping.CanonicalCode}'.");
        if (errors.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        return new(dishes, ingredients, mappings);
    }

    public async Task<ImportSummary> ImportAsync(ValidatedDiscoverySeed seed, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow; var inserted = 0; var updated = 0; var unchanged = 0; var createdRelations = 0; var removedRelations = 0;
        var canonicalByCode = new Dictionary<string, CanonicalIngredient>(StringComparer.Ordinal);
        foreach (var input in seed.Ingredients)
        {
            var entity = await dbContext.CanonicalIngredients.SingleOrDefaultAsync(x => x.Code == input.Code, ct);
            if (entity is null) { entity = CanonicalIngredient.Create(Guid.NewGuid(), input.Code, input.Name, input.Category); dbContext.Add(entity); inserted++; }
            else if (entity.Name != input.Name || entity.NormalizedName != input.NormalizedName || entity.Category != input.Category) { entity.Update(input.Name, input.NormalizedName, input.Category, now); updated++; }
            else unchanged++;
            canonicalByCode[input.Code] = entity;
        }
        await dbContext.SaveChangesAsync(ct);
        var mappingBySource = seed.Mappings.ToDictionary(x => x.IngredientId, x => canonicalByCode[x.CanonicalCode], StringComparer.Ordinal);
        foreach (var input in seed.Dishes)
        {
            var dish = await dbContext.CommonDishes.SingleOrDefaultAsync(x => x.ExternalId == input.Id, ct);
            if (dish is null) { dish = CommonDish.Create(Guid.NewGuid(), input.Id, input.Name, input.Category); dbContext.Add(dish); inserted++; }
            else if (dish.Name != input.Name || dish.NormalizedName != input.NormalizedName || dish.Category != input.Category) { dish.Update(input.Name, input.NormalizedName, input.Category, now); updated++; }
            else unchanged++;
            await dbContext.SaveChangesAsync(ct);
            var desired = input.Ingredients.Select(x => mappingBySource[x.Id].Id).ToHashSet();
            var current = await dbContext.CommonDishIngredients.Where(x => x.DishId == dish.Id).ToListAsync(ct);
            foreach (var obsolete in current.Where(x => !desired.Contains(x.CanonicalIngredientId))) { dbContext.Remove(obsolete); removedRelations++; }
            var currentIds = current.Select(x => x.CanonicalIngredientId).ToHashSet();
            foreach (var ingredientId in desired.Where(x => !currentIds.Contains(x))) { dbContext.Add(new CommonDishIngredient(dish.Id, ingredientId)); createdRelations++; }
        }
        await dbContext.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return new(inserted, updated, unchanged, createdRelations, removedRelations, 0);
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken ct) => JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path, ct), JsonOptions) ?? throw new InvalidDataException($"'{path}' is empty.");
    private static void CheckUnique(IEnumerable<string> values, string label, ICollection<string> errors)
    {
        foreach (var duplicate in values.GroupBy(x => x, StringComparer.Ordinal).Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)) errors.Add($"Duplicate or blank {label}: '{duplicate.Key}'.");
    }
}
