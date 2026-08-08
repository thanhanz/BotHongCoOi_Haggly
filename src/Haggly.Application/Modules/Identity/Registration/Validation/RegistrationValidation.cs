using Haggly.Application.Modules.Identity.Registration.Exceptions;

namespace Haggly.Application.Modules.Identity.Registration.Validation;

internal static class RegistrationValidation
{
    public static void ValidateCommon(
        string email,
        string phoneNumber,
        string password,
        string fullName)
    {
        if (string.IsNullOrWhiteSpace(email)
            || email.Any(char.IsWhiteSpace)
            || email.IndexOf('@') <= 0
            || email.LastIndexOf('.') <= email.IndexOf('@') + 1
            || email.EndsWith(".", StringComparison.Ordinal))
        {
            throw new RegistrationValidationException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new RegistrationValidationException("A phone number is required.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new RegistrationValidationException("Password must be at least 8 characters.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new RegistrationValidationException("Full name is required.");
    }

    public static void ValidateVendor(string businessName)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new RegistrationValidationException("Business name is required.");
    }
}
