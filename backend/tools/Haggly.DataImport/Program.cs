using Haggly.DataImport;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

const string connectionEnvironmentVariable = "HAGGLY_CONNECTION_STRING";

if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    PrintHelp();
    return args.Length == 0 ? 2 : 0;
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    return args[0] switch
    {
        "migrate" => await MigrateAsync(cancellation.Token),
        "seed-reference" or "import" => await SeedReferenceAsync(cancellation.Token),
        "validate-reference" or "validate" => await ValidateReferenceAsync(cancellation.Token),
        _ => UnknownCommand(args[0])
    };
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("Command cancelled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

async Task<int> MigrateAsync(CancellationToken cancellationToken)
{
    await using var context = CreateDbContext();
    await context.Database.MigrateAsync(cancellationToken);
    Console.WriteLine("Database migrations applied successfully.");
    return 0;
}

async Task<int> SeedReferenceAsync(CancellationToken cancellationToken)
{
    var seed = await ValidateSeedAsync(cancellationToken);
    await using var context = CreateDbContext();
    var summary = await new DiscoverySeedImportRepository(context).ImportAsync(seed, cancellationToken);
    Console.WriteLine($"Inserted={summary.Inserted}; Updated={summary.Updated}; Unchanged={summary.Unchanged}; CreatedRelations={summary.CreatedRelations}; RemovedRelations={summary.RemovedRelations}; Rejected={summary.Rejected}");
    return 0;
}

async Task<int> ValidateReferenceAsync(CancellationToken cancellationToken)
{
    await ValidateSeedAsync(cancellationToken);
    return 0;
}

async Task<ValidatedDiscoverySeed> ValidateSeedAsync(CancellationToken cancellationToken)
{
    var seedDirectory = ResolveSeedDirectory(Option("--seed-dir"));
    var seed = await DiscoverySeedImportRepository.ValidateAsync(seedDirectory, cancellationToken);
    Console.WriteLine($"Validated {seed.Dishes.Count} dishes, {seed.Ingredients.Count} canonical ingredients, and {seed.Mappings.Count} source mappings.");
    return seed;
}

HagglyDbContext CreateDbContext()
{
    var connection = Option("--connection") ?? Environment.GetEnvironmentVariable(connectionEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(connection))
    {
        throw new InvalidOperationException(
            $"Provide --connection or set {connectionEnvironmentVariable}.");
    }

    var options = new DbContextOptionsBuilder<HagglyDbContext>()
        .UseNpgsql(connection)
        .Options;
    return new HagglyDbContext(options);
}

static string ResolveSeedDirectory(string? configuredPath)
{
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.GetFullPath(configuredPath);
    }

    var currentDirectory = Directory.GetCurrentDirectory();
    var candidates = new[]
    {
        Path.Combine(currentDirectory, "seed"),
        Path.Combine(currentDirectory, "backend", "seed")
    };

    return candidates.FirstOrDefault(Directory.Exists)
        ?? throw new DirectoryNotFoundException(
            "Could not find the seed directory. Run the command from the repository root or backend directory, or provide --seed-dir.");
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintHelp();
    return 2;
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        Haggly database administration

        Usage:
          dotnet haggly migrate [--connection <connection-string>]
          dotnet haggly seed-reference [--seed-dir <path>] [--connection <connection-string>]
          dotnet haggly validate-reference [--seed-dir <path>]

        Connection:
          Set HAGGLY_CONNECTION_STRING instead of passing --connection to avoid
          storing a database password in shell history.
        """);
}
