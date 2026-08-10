using Haggly.Application.Modules.Identity.Login.Commands;
using Haggly.Application.Modules.Identity.Login.Dtos;

namespace Haggly.Application.Abstractions.Identity;

public interface ILoginUseCase
{
    Task<LoginResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken);
}
