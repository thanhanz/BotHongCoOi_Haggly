using Haggly.Application.Modules.Identity.Login.Exceptions;

namespace Haggly.Application.Modules.Identity.Login.Validation;

internal static class LoginValidation
{
    public static void Validate(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email)
            || email.Any(char.IsWhiteSpace)
            || email.IndexOf('@') <= 0
            || email.LastIndexOf('.') <= email.IndexOf('@') + 1
            || email.EndsWith(".", StringComparison.Ordinal))
        {
            throw new LoginValidationException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
            throw new LoginValidationException("Password is required.");
    }
}
