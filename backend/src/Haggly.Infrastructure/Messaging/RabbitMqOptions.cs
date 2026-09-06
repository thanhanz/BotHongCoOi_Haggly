namespace Haggly.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = string.Empty;
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Host)
            && Port > 0
            && !string.IsNullOrWhiteSpace(VirtualHost)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password);
}
