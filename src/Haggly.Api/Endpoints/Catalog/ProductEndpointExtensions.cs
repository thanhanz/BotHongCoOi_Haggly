using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Catalog.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Catalog.Commands.Products;
using Haggly.Application.Modules.Catalog.Dtos.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Haggly.Api.Endpoints.Catalog;

public static class ProductEndpointExtensions
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(ProductRoutes.Prefix)
            .WithTags("Products")
            .RequireAuthorization();

        group.MapPost(string.Empty, CreateProductAsync)
            .RequireAuthorization(IdentityPolicies.CatalogContributor)
            .Produces<ApiResponse<ProductDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetProductsAsync)
            .Produces<ApiResponse<IReadOnlyCollection<ProductDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet(ProductRoutes.ById, GetProductByIdAsync)
            .Produces<ApiResponse<ProductDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateProductCommand(
                request.CategoryId,
                request.Name,
                request.Description,
                request.DefaultUnit,
                request.ImageUrl),
            cancellationToken);

        return Results.Created(
            $"{ProductRoutes.Prefix}/{result.Id}",
            ApiResponse<ProductDto>.Create(result, "Product created successfully."));
    }

    private static async Task<IResult> GetProductsAsync(
        Guid? categoryId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductsQuery(categoryId), cancellationToken);

        return Results.Ok(
            ApiResponse<IReadOnlyCollection<ProductDto>>.Create(
                result,
                "Products retrieved successfully."));
    }

    private static async Task<IResult> GetProductByIdAsync(
        Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);

        return Results.Ok(
            ApiResponse<ProductDto>.Create(result, "Product retrieved successfully."));
    }
}
