using Haggly.Application.Modules.Identity.Login.Dtos;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Abstractions.Identity;

public interface IIdentityTokenService
{
    IssuedAccessToken CreateAccessToken(
        User user,
        IReadOnlyCollection<RoleCode> roles);
}
