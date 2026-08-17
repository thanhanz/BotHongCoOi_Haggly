using Haggly.Application.Modules.Identity.Login.Exceptions;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Sales.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Haggly.Api.Middleware;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception exception)
        => exception switch
        {
            RegistrationValidationException or LoginValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            AuthenticationException
                => (StatusCodes.Status401Unauthorized, "Authentication failed", exception.Message),
            RegistrationConflictException
                => (StatusCodes.Status409Conflict, "Registration conflict", exception.Message),
            VendorQueryValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            VendorCommandValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            VendorNotFoundException
                => (StatusCodes.Status404NotFound, "Vendor not found", exception.Message),
            VendorTransitionConflictException
                => (StatusCodes.Status409Conflict, "Vendor conflict", exception.Message),
            MarketValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            MarketConflictException
                => (StatusCodes.Status409Conflict, "Market conflict", exception.Message),
            MarketNotFoundException
                => (StatusCodes.Status404NotFound, "Market not found", exception.Message),
            StallValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            StallConflictException
                => (StatusCodes.Status409Conflict, "Stall conflict", exception.Message),
            StallNotFoundException
                => (StatusCodes.Status404NotFound, "Stall not found", exception.Message),
            CategoryValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            CategoryConflictException
                => (StatusCodes.Status409Conflict, "Category conflict", exception.Message),
            CategoryNotFoundException
                => (StatusCodes.Status404NotFound, "Category not found", exception.Message),
            ProductValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            ProductConflictException
                => (StatusCodes.Status409Conflict, "Product conflict", exception.Message),
            ProductNotFoundException
                => (StatusCodes.Status404NotFound, "Product not found", exception.Message),
            ProductStallValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            ProductStallForbiddenException
                => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            ProductStallConflictException
                => (StatusCodes.Status409Conflict, "Product-stall conflict", exception.Message),
            ProductStallNotFoundException
                => (StatusCodes.Status404NotFound, "Stall product not found", exception.Message),
            InventoryValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            InventoryForbiddenException
                => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            InventoryNotFoundException
                => (StatusCodes.Status404NotFound, "Inventory resource not found", exception.Message),
            InventoryConflictException
                => (StatusCodes.Status409Conflict, "Inventory conflict", exception.Message),
            PosSaleValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            PosSaleForbiddenException
                => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            PosSaleNotFoundException
                => (StatusCodes.Status404NotFound, "POS sale resource not found", exception.Message),
            PosSaleConflictException
                => (StatusCodes.Status409Conflict, "POS sale conflict", exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing the request.")
        };
}
