# Architecture Update 44

## Conventional short-URL redirects

Entity endpoint groups with canonical `/list` and `/view/{id}` routes now expose two convenience aliases:

- `/{app}/{TypeResponse}` -> `/{app}/{TypeResponse}/list`
- `/{app}/{TypeResponse}/{id}` -> `/{app}/{TypeResponse}/view/{id}`

The aliases are implemented by the shared `WebAppArchitecture.Base.Resolvers.RedirectResolvers` helper rather than by duplicating entity resolver mappings. Redirects are temporary method-preserving redirects and preserve the complete query string, including vector-page state such as `where`, `order_by`, `take` and `skip`.

The convention is currently wired for User, Group, Membership, Role and RoleType endpoint groups, all of which already expose canonical `/list` and `/view/{id}` endpoints. GroupType is intentionally unchanged because its current endpoint contract still uses `/read/{id}` rather than `/view/{id}`.

## Relation entity visualization

Membership and Role are relation entities and are now visualized primarily through their neighbouring entities instead of GUIDs.

`MembershipEntityLabel` renders:

`UserName @ GroupName`

`RoleEntityLabel` renders:

`UserName @ GroupName [RoleTypeName]`

Each part is a lazy `EntityLink` resolved through the existing secured `/link/{id}` endpoint. This avoids treating denormalized names on Membership/Role rows as source-of-truth data and ensures that labels of neighbouring entities are disclosed only through their normal secured read/link path.

The User link label uses `Name + surname`; when the composed name is empty, `email` is used as fallback.

## Vector table entity-label component

`VectorContentViewModel` now has optional `ItemLabelComponentType`. `TableVectorContent` uses that component for the first entity column when supplied; otherwise it retains the previous `visual_label` link behavior.

User/Group Membership vectors use `MembershipEntityLabel`, and User/Group Role vectors use `RoleEntityLabel`. Their remaining table columns therefore focus on actual relation attributes such as validity and deputy status rather than duplicating neighbouring entity names.

No changes were made to the meaning or naming of `EntityLink`, `MediumCard`, `LargeCard`, or the generic page/vector composition introduced previously.
