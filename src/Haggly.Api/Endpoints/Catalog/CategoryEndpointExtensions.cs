using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Catalog.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Commands.Categories;
using Haggly.Application.Modules.Catalog.Dtos.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Haggly.Api.Endpoints.Catalog;

public static class CategoryEndpointExtensions
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(CategoryRoutes.Prefix)
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapPost(string.Empty, CreateCategoryAsync)
            .RequireAuthorization(IdentityPolicies.CatalogContributor)
            .Produces<ApiResponse<CategoryDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetCategoriesAsync)
            .Produces<ApiResponse<PagedResult<CategoryDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet(CategoryRoutes.ById, GetCategoryByIdAsync)
            .Produces<ApiResponse<CategoryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateCategoryAsync(
        CreateCategoryRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCategoryCommand(
                request.Name,
                request.Slug,
                request.Description,
                request.ImageUrl,
                request.ParentCategoryId,
                request.DisplayOrder),
            cancellationToken);

        return Results.Created(
            $"{CategoryRoutes.Prefix}/{result.Id}",
            ApiResponse<CategoryDto>.Create(result, "Category created successfully."));
    }

    private static async Task<IResult> GetCategoriesAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCategoriesQuery(page ?? 1, pageSize ?? 20),
            cancellationToken);

        return Results.Ok(
            ApiResponse<PagedResult<CategoryDto>>.Create(
                result,
                "Categories retrieved successfully."));
    }

    private static async Task<IResult> GetCategoryByIdAsync(
        Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoryByIdQuery(id), cancellationToken);

        return Results.Ok(
            ApiResponse<CategoryDto>.Create(result, "Category retrieved successfully."));
    }
}
