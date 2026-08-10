using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Domain.Modules.Negotiation;

public sealed class NegotiationMessage : ImmutableEntity
{
    public Guid NegotiationSessionId { get; set; }
    public Guid SenderUserId { get; set; }
    public NegotiationMessageType MessageType { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }

    public NegotiationSession? NegotiationSession { get; set; }
    public User? SenderUser { get; set; }
}
