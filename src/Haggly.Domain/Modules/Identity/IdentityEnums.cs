namespace Haggly.Domain.Modules.Identity;

public enum UserStatus
{
    Active,
    Suspended,
    Pending
}

public enum RoleCode
{
    Buyer,
    Vendor,
    MarketAdmin,
    PlatformAdmin,
    Deliverer
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Suspended
}

public enum AdminScope
{
    Market,
    Platform
}

public enum VehicleType
{
    Motorbike,
    Car,
    Truck,
    Other
}
