using Haggly.Application.Modules.Markets.Commands.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Modules.Markets.Validation.Markets;

internal static class MarketValidation
{
    public static void Validate(CreateMarketCommand command)
    {
        ValidateFields(command.Code, command.Name, command.Address);
        ValidateCoordinates(command.Latitude, command.Longitude);
        ValidateOpeningHours(command.OpeningTime, command.ClosingTime);
    }

    public static void Validate(UpdateMarketCommand command)
    {
        if (command.Id == Guid.Empty)
            throw new MarketValidationException("A valid market ID is required.");

        ValidateFields(command.Code, command.Name, command.Address);
        ValidateCoordinates(command.Latitude, command.Longitude);
        ValidateOpeningHours(command.OpeningTime, command.ClosingTime);

        if (!Enum.IsDefined(command.Status))
            throw new MarketValidationException("A valid market status is required.");
    }

    private static void ValidateFields(string code, string name, string address)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 50)
            throw new MarketValidationException("Market code is required and must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new MarketValidationException("Market name is required and must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(address) || address.Length > 500)
            throw new MarketValidationException("Market address is required and must not exceed 500 characters.");
    }

    private static void ValidateCoordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude is < -90 or > 90)
            throw new MarketValidationException("Latitude must be between -90 and 90.");

        if (longitude is < -180 or > 180)
            throw new MarketValidationException("Longitude must be between -180 and 180.");
    }

    private static void ValidateOpeningHours(TimeOnly? openingTime, TimeOnly? closingTime)
    {
        if (openingTime is not null && closingTime is not null && openingTime >= closingTime)
            throw new MarketValidationException("Opening time must be earlier than closing time.");
    }
}
