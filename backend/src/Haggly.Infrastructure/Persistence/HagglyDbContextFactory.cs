using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Haggly.Infrastructure.Persistence;

public sealed class HagglyDbContextFactory : IDesignTimeDbContextFactory<HagglyDbContext>
{
    public HagglyDbContext CreateDbContext(string[] args)
    {
        var connectionString = ReadConnectionString(args)
            ?? Environment.GetEnvironmentVariable("HAGGLY_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Provide a database connection with --connection or HAGGLY_CONNECTION_STRING.");

        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new HagglyDbContext(options);
    }

    private static string? ReadConnectionString(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], "--connection", StringComparison.OrdinalIgnoreCase))
                return args[index + 1];
        }

        return null;
    }
}
