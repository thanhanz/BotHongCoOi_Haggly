using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Abstractions.Markets;

public interface IStallQuery
{
    Task<Stall?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Stall>> GetAllAsync(CancellationToken cancellationToken);
}
