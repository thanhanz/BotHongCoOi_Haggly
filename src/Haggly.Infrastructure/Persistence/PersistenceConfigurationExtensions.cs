using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Haggly.Application.Modules.Identity.Registration;
using Haggly.Application.Modules.Identity.Login;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Login.Commands;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Modules.Finance.Events.V1;
using Haggly.Application.Modules.Inventory.Events.V1;
using Haggly.Infrastructure.Authentication;
using Haggly.Infrastructure.MediatR;
using Haggly.Infrastructure.Persistence.Repositories.Identity;
using Haggly.Infrastructure.Persistence.Repositories.Catalog;
using Haggly.Infrastructure.Persistence.Repositories.Markets;
using Haggly.Infrastructure.Persistence.Queries.Catalog;
using Haggly.Infrastructure.Persistence.Queries.Markets;
using Haggly.Infrastructure.Persistence.Queries.Identity;
using Haggly.Infrastructure.Persistence.Queries.Inventory;
using Haggly.Infrastructure.Persistence.Repositories.Inventory;
using Haggly.Infrastructure.Persistence.Repositories.Sales;
using Haggly.Infrastructure.Persistence.Queries.Sales;
using Haggly.Infrastructure.Persistence.Repositories.Finance;
using Haggly.Infrastructure.Persistence.Repositories.Payments;

namespace Haggly.Infrastructure.Persistence;

public static class PersistenceConfigurationExtensions
{
    private const string ConnectionStringName = "HagglyDatabase";

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConfigurationPath.Combine("ConnectionStrings", ConnectionStringName)}' is required.");
        }

        services.AddDbContext<HagglyDbContext>(options => options.UseNpgsql(connectionString));
        services.AddInfrastructureRepositories();

        return services;
    }

    //TODO: Consider moving this to a separate class for better organization and maintainability.
    // Command, Repository, and Query registrations can be grouped by module or feature for clarity.
    public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
    {
        services.AddHagglyMediatR();
        services.AddScoped<DapperDbContext>();

        services.AddScoped<IIdentityRegistrationRepository, EfIdentityRegistrationRepository>();
        services.AddScoped<IIdentityLoginRepository, EfIdentityLoginRepository>();
        services.AddScoped<IVendorAdminCommandRepository, EfVendorAdminCommandRepository>();
        services.AddScoped<ICategoryCommandRepository, EfCategoryCommandRepository>();
        services.AddScoped<IProductCommandRepository, EfProductCommandRepository>();
        services.AddScoped<IProductStallCommandRepository, EfProductStallCommandRepository>();
        services.AddScoped<IMarketCommandRepository, EfMarketCommandRepository>();
        services.AddScoped<IStallCommandRepository, EfStallCommandRepository>();

        services.AddScoped<IInventoryCommandRepository, EfInventoryCommandRepository>();
        services.AddScoped<IInventoryReferenceQuery, EfInventoryReferenceQuery>();
        services.AddScoped<IInventoryUnitOfWork, EfInventoryUnitOfWork>();
        services.AddScoped<IInventorySaleRepository, EfInventorySaleRepository>();
        services.AddScoped<IInventoryPaymentRepository, EfInventoryPaymentRepository>();
        services.AddScoped<InventoryPaymentSucceededHandler>();
        services.AddScoped<IPosSaleCommandRepository, EfPosSaleCommandRepository>();
        services.AddScoped<IPosSaleUnitOfWork, EfPosSaleUnitOfWork>();
        services.AddScoped<IOrderCommandRepository, EfOrderCommandRepository>();
        services.AddScoped<ICartCommandRepository, EfCartCommandRepository>();
        services.AddScoped<ICartCheckoutUnitOfWork, EfCartCheckoutUnitOfWork>();
        services.AddScoped<IPaymentCommandRepository, EfPaymentCommandRepository>();
        services.AddScoped<IPaymentUnitOfWork, EfPaymentUnitOfWork>();
        services.AddScoped<IPaymentAllocationRepository, EfPaymentAllocationRepository>();

        services.AddScoped<IPosSaleQuery, DapperPosSaleQuery>();
        services.AddScoped<IOrderQuery, DapperOrderQuery>();
        services.AddScoped<IOrderCatalog, DapperOrderCatalog>();
        services.AddScoped<ICartQuery, DapperCartQuery>();
        services.AddScoped<ICartCatalog, DapperCartCatalog>();
        services.AddScoped<IRevenueLedgerRepository, EfRevenueLedgerRepository>();
        services.AddScoped<FinancePaymentSucceededHandler>();

        services.AddScoped<IMarketQuery, DapperMarketQuery>();
        services.AddScoped<ICategoryQuery, DapperCategoryQuery>();
        services.AddScoped<IProductQuery, DapperProductQuery>();
        services.AddScoped<IProductStallQuery, DapperProductStallQuery>();
        services.AddScoped<IStallQuery, DapperStallQuery>();
        services.AddScoped<IVendorAdminQuery, DapperVendorAdminQuery>();
        services.AddScoped<IInventoryQuery, DapperInventoryQuery>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<RegisterBuyerHandler>();
        services.AddScoped<RegisterVendorHandler>();
        
        // Register strategy handlers for use cases
        services.AddScoped<IRegisterBuyerUseCase>(provider =>
            provider.GetRequiredService<RegisterBuyerHandler>());
        services.AddScoped<IRegisterVendorUseCase>(provider =>
            provider.GetRequiredService<RegisterVendorHandler>());
        services.AddScoped<LoginHandler>();
        services.AddScoped<ILoginUseCase>(provider =>
            provider.GetRequiredService<LoginHandler>());

        return services;
    }
}
