namespace Haggly.Application.Abstractions.Identity;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
}
