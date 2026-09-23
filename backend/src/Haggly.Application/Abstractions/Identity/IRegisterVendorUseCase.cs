using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Dtos;

namespace Haggly.Application.Abstractions.Identity;

public interface IRegisterVendorUseCase
{
    Task<RegistrationResult> HandleAsync(
        RegisterVendorCommand command,
        CancellationToken cancellationToken);
}
