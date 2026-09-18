namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record CommonDishSearchResult(
    IReadOnlyList<CommonDishResult> Candidates);
