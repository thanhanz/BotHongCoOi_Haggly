using Haggly.Domain.Modules.Markets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haggly.Application.Abstractions.Markets;

public interface IMarketQuery
{
  Task<Market?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

  Task<IReadOnlyCollection<Market>> GetAllAsync(CancellationToken cancellationToken);
}

