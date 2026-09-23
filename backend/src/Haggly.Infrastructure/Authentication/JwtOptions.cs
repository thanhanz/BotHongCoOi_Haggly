namespace Haggly.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Issuer)
            && !string.IsNullOrWhiteSpace(Audience)
            && SigningKey.Length >= 32
            && AccessTokenMinutes is >= 1 and <= 1440;
}
