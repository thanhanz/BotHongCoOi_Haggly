# Identity Module

## Ownership

Identity owns:

- User
- Role
- UserRole
- BuyerProfile
- VendorProfile
- AdminProfile
- DelivererProfile (temporarily not implement)

Identity does not own:

- Stall ownership
- Orders
- Inventory
- Payments
- Revenue

Those belong to their respective modules.

## Roles

Supported roles:

- Buyer
- Vendor
- Admin
- Deliverer

A user may have multiple roles if the business allows it.

## Authorization Rules

### Buyer

Can:

- browse products
- create orders
- negotiate their own orders
- pay for their own orders
- view their own orders

Cannot:

- modify another buyer's order
- modify vendor inventory
- manage stalls

### Vendor

Can:

- manage stalls they own or operate
- manage products/inventory for those stalls
- view orders belonging to those stalls
- negotiate orders belonging to those stalls
- mark their own stall fulfillment as ready

Cannot:

- modify another vendor's stall
- modify another vendor's fulfillment
- access unrelated buyer data

### Admin

Can:

- perform administrative operations explicitly exposed to admins (Full access)

Admin access must not automatically bypass every domain rule.

### Deliverer

Reserved for the delivery phase.

Do not implement delivery permissions until the delivery feature is introduced.

## Ownership Authorization

Role authorization and resource ownership are different.

Example:

A Vendor role does not mean the vendor can edit every stall.

Required:

Vendor role
AND
vendor owns/manages the requested stall

Stall ownership belongs to MarketManagement, not Identity.

## Authentication Boundary

Identity defines application users and roles.

Authentication infrastructure determines how a user proves their identity.

Do not assume JWT, cookies, OAuth, or a specific provider unless selected elsewhere.

Application code should obtain the current authenticated user through an abstraction such as:

IUserContext

Do not trust a UserId, BuyerId, or VendorId supplied by the client when the authenticated user's identity should be used.

## Security Rules

Never log:

- passwords
- password hashes
- access tokens
- refresh tokens
- authorization headers
- authentication secrets

## Required Tests

When changing Identity or authorization, test:

- role restrictions
- resource ownership checks
- unauthenticated access
- forbidden access
- account/profile state rules