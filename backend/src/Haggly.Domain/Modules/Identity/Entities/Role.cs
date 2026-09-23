using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Identity;

public sealed class Role : SoftDeletableEntity
{
    public RoleCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
