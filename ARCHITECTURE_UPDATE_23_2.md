# Architecture Update 23.2

- RbacPermissionList now renders entity references as links.
- User label links to UserResponse/view/{id}.
- Group label links to GroupResponse/view/{id}.
- Role points to RoleResponse/view/{id}.
- Role type links to RoleTypeResponse/view/{id}.
- source_membership_id links to MembershipResponse/view/{id}.
- Added role_type_id to RbacPermissionItemResponse so the Razor UI can link the role type without deriving identifiers from labels.
- No database schema or runtime Security.Query changes.
