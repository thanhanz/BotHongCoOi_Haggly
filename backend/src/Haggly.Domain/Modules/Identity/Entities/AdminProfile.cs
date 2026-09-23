using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Identity;

public sealed class AdminProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public string? EmployeeCode { get; set; }
    public AdminScope AdminScope { get; set; }
}
