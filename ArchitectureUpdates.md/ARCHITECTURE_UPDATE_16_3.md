# Architecture Update 16.3

Fixes DTO -> DB model constructor calls after EF-safe DB model constructor changes.

The DB constructors for GroupTypeModel, GroupModel, MembershipModel and RoleModel contain only persisted properties. DTO helper/display values are now assigned through object initializers to their NotMapped properties.

Updated call sites:
- GroupTypeResponse.ToModel()
- GroupResponse.ToModel()
- MembershipResponse.ToModel()
- RoleResponse.ToModel()

Create/update request constructors already matched the persisted constructor signatures and required no equivalent display-property arguments.
