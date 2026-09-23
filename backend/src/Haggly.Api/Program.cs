using Haggly.Api;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Authentication;
using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Endpoints.Markets;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Api.Endpoints.Inventory;
using Haggly.Api.Endpoints.Sales;
using Haggly.Api.Endpoints.Payments;
using Haggly.Api.Endpoints.Finance;
using Haggly.Api.Endpoints.Discovery;
using Haggly.Infrastructure.Messaging;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Payments;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddMessaging(builder.Configuration);

        builder.Services.AddPaymentProvider(builder.Configuration);
        
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();

        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? throw new InvalidOperationException("Cors:AllowedOrigins must be configured.");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            await ApplicationDataSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<HagglyDbContext>(),
                scope.ServiceProvider.GetRequiredService<Haggly.Application.Abstractions.Identity.IPasswordHasher>());
            if (app.Configuration.GetValue<bool>($"{OutboxBenchmarkOptions.SectionName}:Enabled"))
            {
                await scope.ServiceProvider
                    .GetRequiredService<OutboxMessageStimulateInit>()
                    .SeedDataAsync();
            }
        }

        //Middleware start here
        app.UseExceptionHandler();
        app.UseCors("Frontend");
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
        app.MapProductListingEndpoints();
        app.MapPosSaleEndpoints();
        app.MapCartEndpoints();
        app.MapOrderEndpoints();
        app.MapPaymentEndpoints();
        app.MapRevenueReportEndpoints();
        app.MapDiscoveryEndpoints();
        app.MapMarketplaceSearchEndpoints();

        app.Run();
    }
}
