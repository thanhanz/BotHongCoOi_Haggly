using Haggly.DataImport;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

if (args.Length == 0 || args[0] is not ("validate" or "import"))
{
    Console.Error.WriteLine("Usage: Haggly.DataImport <validate|import> [--seed-dir <path>] [--connection <connection-string>]");
    return 2;
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

try
{
    var seedDirectory = Path.GetFullPath(Option("--seed-dir") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "seed"));
    var seed = await DiscoverySeedImportRepository.ValidateAsync(seedDirectory, CancellationToken.None);
    Console.WriteLine($"Validated {seed.Dishes.Count} dishes, {seed.Ingredients.Count} canonical ingredients, and {seed.Mappings.Count} source mappings.");
    if (args[0] == "validate") return 0;
    var connection = Option("--connection") ?? Environment.GetEnvironmentVariable("HAGGLY_CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Provide --connection or HAGGLY_CONNECTION_STRING for import.");
    var options = new DbContextOptionsBuilder<HagglyDbContext>().UseNpgsql(connection).Options;
    await using var context = new HagglyDbContext(options);
    var summary = await new DiscoverySeedImportRepository(context).ImportAsync(seed, CancellationToken.None);
    Console.WriteLine($"Inserted={summary.Inserted}; Updated={summary.Updated}; Unchanged={summary.Unchanged}; CreatedRelations={summary.CreatedRelations}; RemovedRelations={summary.RemovedRelations}; Rejected={summary.Rejected}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
