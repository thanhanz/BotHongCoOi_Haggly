using Haggly.Api;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Authentication;
using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Endpoints.Markets;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Api.Endpoints.Inventory;
using Haggly.Api.Endpoints.Sales;
using Haggly.Infrastructure.Messaging;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddMessaging(builder.Configuration);
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            await DevelopmentAdminSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<HagglyDbContext>(),
                scope.ServiceProvider.GetRequiredService<Haggly.Application.Abstractions.Identity.IPasswordHasher>());
        }

        //Middleware start here
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerDocumentation();
        }

        app.MapIdentityEndpoints();
        app.MapVendorAdminEndpoints();
        app.MapMarketEndpoints();
        app.MapStallEndpoints();
        app.MapCategoryEndpoints();
        app.MapProductEndpoints();
        app.MapProductStallEndpoints();
        app.MapInventoryEndpoints();
        app.MapPosSaleEndpoints();
        app.MapCartEndpoints();
        app.MapOrderEndpoints();

        app.Run();
    }
}
