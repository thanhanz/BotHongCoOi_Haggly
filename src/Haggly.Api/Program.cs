using Haggly.Infrastructure.Persistence;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPersistence(builder.Configuration);

        var app = builder.Build();

        app.Run();
    }
}