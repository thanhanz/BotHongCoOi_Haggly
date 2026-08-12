using System.Text.Json.Serialization;

namespace Haggly.Domain.Modules.Identity;

[JsonConverter(typeof(JsonStringEnumConverter<ApprovalStatus>))]
public enum ApprovalStatus
{
    PENDING,
    APPROVED,
    REJECTED,
    SUSPENDED
}
