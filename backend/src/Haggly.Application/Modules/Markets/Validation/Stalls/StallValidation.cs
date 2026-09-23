using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;

namespace Haggly.Application.Modules.Markets.Validation.Stalls;

internal static class StallValidation
{
    public static void Validate(CreateStallCommand command)
    {
        ValidateIds(command.MarketId, command.VendorId);
        if (command.ActorUserId == Guid.Empty)
            throw new StallValidationException("A valid actor ID is required.");
        ValidateFields(command.Code, command.Name, command.LocationDescription, command.PhoneNumber);
    }

    public static void Validate(UpdateStallCommand command)
    {
        if (command.Id == Guid.Empty)
            throw new StallValidationException("A valid stall ID is required.");

        ValidateIds(command.MarketId, command.VendorId);
        ValidateFields(command.Code, command.Name, command.LocationDescription, command.PhoneNumber);

        if (!Enum.IsDefined(command.Status))
            throw new StallValidationException("A valid stall status is required.");
    }

    private static void ValidateIds(Guid marketId, Guid vendorId)
    {
        if (marketId == Guid.Empty)
            throw new StallValidationException("A valid market ID is required.");

        if (vendorId == Guid.Empty)
            throw new StallValidationException("A valid vendor ID is required.");
    }

    private static void ValidateFields(
        string code,
        string name,
        string? locationDescription,
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 50)
            throw new StallValidationException("Stall code is required and must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new StallValidationException("Stall name is required and must not exceed 200 characters.");

        if (locationDescription?.Length > 500)
            throw new StallValidationException("Location description must not exceed 500 characters.");

        if (phoneNumber?.Length > 32)
            throw new StallValidationException("Phone number must not exceed 32 characters.");
    }
}
