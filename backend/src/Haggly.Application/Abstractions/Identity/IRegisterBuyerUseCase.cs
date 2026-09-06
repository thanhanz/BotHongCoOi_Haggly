using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Dtos;

namespace Haggly.Application.Abstractions.Identity;

public interface IRegisterBuyerUseCase
{
    Task<RegistrationResult> HandleAsync(
        RegisterBuyerCommand command,
        CancellationToken cancellationToken);
}
