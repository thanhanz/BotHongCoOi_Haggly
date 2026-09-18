using Haggly.Api.Authorization;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Discovery;
using Haggly.Application.Modules.Discovery.Commands;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Application.Modules.Discovery.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Discovery;

public sealed record SetProductIngredientMappingRequest(Guid? CanonicalIngredientId, string? Status);

public static class DiscoveryEndpointExtensions
{
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var dishes = endpoints.MapGroup(DiscoveryRoutes.CommonDishes).WithTags("Dish Discovery");
        dishes.MapGet(DiscoveryRoutes.Search, SearchAsync).AllowAnonymous().Produces<ApiResponse<CommonDishSearchResult>>().ProducesProblem(400);
        dishes.MapGet(DiscoveryRoutes.Proposal, ProposalAsync).AllowAnonymous().Produces<ApiResponse<DishProposalResult>>().ProducesProblem(400).ProducesProblem(404);
        endpoints.MapGet(DiscoveryRoutes.CanonicalIngredients, FindIngredientsAsync).WithTags("Dish Discovery").RequireAuthorization(IdentityPolicies.CatalogContributor).Produces<ApiResponse<IReadOnlyList<CanonicalIngredientResult>>>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403);
        endpoints.MapPatch(DiscoveryRoutes.ProductMapping, SetMappingAsync).WithTags("Dish Discovery").RequireAuthorization(IdentityPolicies.AdminOnly).Produces(StatusCodes.Status204NoContent).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);
        return endpoints;
    }
    
    private static async Task<IResult> SearchAsync([FromQuery] string? q, ISender sender, CancellationToken ct) 
      => Results.Ok(ApiResponse<CommonDishSearchResult>.Create(await sender.Send(new SearchCommonDishesQuery(q ?? string.Empty), ct), "Dish search completed."));
    private static async Task<IResult> ProposalAsync(Guid dishId, ISender sender, CancellationToken ct) 
      => Results.Ok(ApiResponse<DishProposalResult>.Create(await sender.Send(new GetDishProposalQuery(dishId), ct), "Dish proposal retrieved."));
    private static async Task<IResult> FindIngredientsAsync([FromQuery] string? q, ISender sender, CancellationToken ct) 
      => Results.Ok(ApiResponse<IReadOnlyList<CanonicalIngredientResult>>
                .Create(await sender.Send(new FindCanonicalIngredientsQuery(q ?? string.Empty), ct), "Canonical ingredients retrieved."));
    private static async Task<IResult> SetMappingAsync(Guid productId, SetProductIngredientMappingRequest request, ISender sender, CancellationToken ct) 
    { await sender.Send(new SetProductIngredientMappingCommand(productId, request.CanonicalIngredientId, request.Status), ct); 
      return Results.NoContent(); }
}
