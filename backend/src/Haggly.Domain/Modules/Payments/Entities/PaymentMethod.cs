using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Payments;

public sealed class PaymentMethod : SoftDeletableEntity
{
    public PaymentMethodCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConfigurationJson { get; set; }
}
