using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Authentication;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddTokenServices(builder.Configuration);

        var app = builder.Build();

        app.Run();
    }
}
