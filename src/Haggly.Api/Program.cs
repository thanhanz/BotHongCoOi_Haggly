using Haggly.Api;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Authentication;
using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Endpoints.Markets;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();

        var app = builder.Build();

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

        app.Run();
    }
}
