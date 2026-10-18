using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Events;
using WebAppArchitecture.Facilities;
using Xunit;

namespace Facilities.Integration.Tests;

public sealed class FacilitiesRelationalTests(FacilitiesPostgresFixture fixture)
    : IClassFixture<FacilitiesPostgresFixture>
{
    [Fact]
    public async Task TreeCreationRelationsAncestorsDescendantsAndTypeContainmentPersistInPostgres()
    {
        var actorId = await fixture.CreateActorAsync("Tree manager");
        var facilityScope = await fixture.CreateScopeAsync("Tree facilities");
        var typeScope = await fixture.CreateScopeAsync("Tree types");
        await fixture.GrantAsync(actorId, facilityScope, SecurityOperations.Read, SecurityOperations.Create,
            SecurityOperations.Update, SecurityOperations.Delete);
        await fixture.GrantAsync(actorId, typeScope, SecurityOperations.Read, SecurityOperations.Create,
            SecurityOperations.Update, SecurityOperations.Delete);
        await fixture.RebuildAccessAsync();

        using var actor = fixture.ForActor(actorId);
        var leafType = await actor.FacilityTypes.CreateAsync(new(
            "LEAF-" + Guid.NewGuid().ToString("N"), "Leaf", null, true, false, [], typeScope));
        var roomType = await actor.FacilityTypes.CreateAsync(new(
            "ROOM-" + Guid.NewGuid().ToString("N"), "Room", null, true, true, [leafType.id], typeScope));
        var buildingType = await actor.FacilityTypes.CreateAsync(new(
            "BUILDING-" + Guid.NewGuid().ToString("N"), "Building", null, false, true, [roomType.id], typeScope));
        Assert.Equal(new[] { leafType.id }, roomType.AllowedChildTypeIds);
        Assert.Equal(new[] { roomType.id }, buildingType.AllowedChildTypeIds);

        var building = await actor.Facilities.CreateAsync(new(
            "Building", null, buildingType.id, null, true, facilityScope, "BLD-01"));
        var room = await actor.Facilities.CreateAsync(new(
            "Room", null, roomType.id, building.id, true, facilityScope, "ROOM-01"));
        var leaf = await actor.Facilities.CreateAsync(new(
            "Leaf", null, leafType.id, room.id, true, facilityScope, "LEAF-01"));

        Assert.Equal($"/{building.id:D}/", building.path);
        Assert.Equal($"{building.path}{room.id:D}/", room.path);
        Assert.Equal($"{room.path}{leaf.id:D}/", leaf.path);
        Assert.Equal(new[] { room.id }, (await actor.Facilities.ListChildrenAsync(building.id)).Select(x => x.id));
        Assert.Equal(new[] { building.id, room.id },
            (await actor.Facilities.ListAncestorsAsync(leaf.id)).Select(x => x.id));
        Assert.Equal(new[] { room.id, leaf.id },
            (await actor.Facilities.ListDescendantsAsync(building.id)).Select(x => x.id));

        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.Facilities.CreateAsync(new(
            "Invalid direct leaf", null, leafType.id, building.id, true, facilityScope, "INVALID-LEAF")));
        Assert.DoesNotContain(await actor.Facilities.ListChildrenAsync(building.id), x => x.Name == "Invalid direct leaf");

        var persisted = await fixture.ReadFacilitiesAsync(building.id, room.id, leaf.id);
        Assert.Equal(3, persisted.Count);
        Assert.Equal(3, persisted.Select(x => x.rbacobject_id).Distinct().Count());
        Assert.DoesNotContain(persisted, x => x.rbacobject_id == facilityScope);
        var ownedGroups = await fixture.ReadGroupsAsync(persisted.Select(x => x.rbacobject_id).ToArray());
        Assert.Equal(facilityScope, ownedGroups.Single(x => x.id == building.rbacobject_id).master_group_id);
        Assert.Equal(building.rbacobject_id, ownedGroups.Single(x => x.id == room.rbacobject_id).master_group_id);
        Assert.Equal(room.rbacobject_id, ownedGroups.Single(x => x.id == leaf.rbacobject_id).master_group_id);
        Assert.All(persisted, facility =>
        {
            var ownScope = ownedGroups.Single(group => group.id == facility.rbacobject_id);
            Assert.Equal(ownScope.id, ownScope.rbacobject_id);
        });

        var duplicateScopeFacility = new FacilityModel
        {
            id = Guid.NewGuid(),
            rbacobject_id = building.rbacobject_id,
            created_at = fixture.Now,
            updated_at = fixture.Now,
            created_by = fixture.AdministratorId,
            updated_by = fixture.AdministratorId,
            FacilityTypeId = building.FacilityTypeId,
            Code = "DUPLICATE-SCOPE-" + Guid.NewGuid().ToString("N"),
            Name = "Duplicate owned scope",
            IsActive = true,
            path = $"/{Guid.NewGuid():D}/"
        };
        await using (var context = fixture.CreateContext())
        {
            context.Set<FacilityModel>().Add(duplicateScopeFacility);
            var failure = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            var postgresFailure = Assert.IsType<PostgresException>(failure.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresFailure.SqlState);
            Assert.Equal("IX_facilities_rbacobject_id", postgresFailure.ConstraintName);
        }
    }

    [Fact]
    public async Task CreatingChildRequiresCreateOnParentAndIgnoresCallerSelectedScope()
    {
        var ownerId = await fixture.CreateActorAsync("Facility parent creator");
        var parentGroup = await fixture.CreateScopeAsync("Facility parent scope");
        var typeScope = await fixture.CreateScopeAsync("Child facility type scope");
        var unrelatedScope = await fixture.CreateScopeAsync("Caller supplied unrelated scope");
        await fixture.GrantAsync(ownerId, parentGroup, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.GrantAsync(ownerId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.GrantAsync(ownerId, unrelatedScope, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        Guid parentId;
        Guid typeId;
        Guid childCreatorId;
        using (var owner = fixture.ForActor(ownerId))
        {
            var type = await owner.FacilityTypes.CreateAsync(new(
                "CHILD-" + Guid.NewGuid().ToString("N"), "Child creation", null, true, true, [], typeScope));
            var parent = await owner.Facilities.CreateAsync(new(
                "Parent", null, type.id, null, true, parentGroup, "CHILD-PARENT"));
            parentId = parent.id;
            typeId = type.id;
            childCreatorId = await fixture.CreateActorAsync("Unrelated-scope-only child creator");
        }

        await fixture.GrantAsync(childCreatorId, typeScope, SecurityOperations.Read);
        await fixture.GrantAsync(childCreatorId, unrelatedScope, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();
        using (var denied = fixture.ForActor(childCreatorId))
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => denied.Facilities.CreateAsync(new(
                "Denied child", null, typeId, parentId, true, unrelatedScope, "CHILD-DENIED")));
        }

        var parentBefore = await fixture.ReadFacilityAsync(parentId);
        Assert.Single(await fixture.ReadFacilitySubtreeAsync(parentBefore.path));
        await fixture.GrantAsync(childCreatorId, parentBefore.rbacobject_id, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();
        using var permitted = fixture.ForActor(childCreatorId);
        var child = await permitted.Facilities.CreateAsync(new(
            "Authorized child", null, typeId, parentId, true, unrelatedScope, "CHILD-OK"));
        Assert.Equal(parentId, child.ParentId);
        Assert.NotEqual(unrelatedScope, child.rbacobject_id);
        Assert.NotEqual(parentBefore.rbacobject_id, child.rbacobject_id);
        var childScope = (await fixture.ReadGroupsAsync(child.rbacobject_id)).Single();
        Assert.Equal(parentBefore.rbacobject_id, childScope.master_group_id);
    }

    [Fact]
    public async Task FacilityReadsAreRbacFilteredAndUnauthorizedCrudOrMoveLeavesRowsUnchanged()
    {
        var ownerId = await fixture.CreateActorAsync("Facility owner");
        var readerId = await fixture.CreateActorAsync("Facility reader");
        var visibleScope = await fixture.CreateScopeAsync("Visible facility scope");
        var hiddenScope = await fixture.CreateScopeAsync("Hidden facility scope");
        var typeScope = await fixture.CreateScopeAsync("Authorization test types");
        await fixture.GrantAsync(ownerId, visibleScope, SecurityOperations.Read, SecurityOperations.Create,
            SecurityOperations.Update, SecurityOperations.Delete);
        await fixture.GrantAsync(ownerId, hiddenScope, SecurityOperations.Read, SecurityOperations.Create,
            SecurityOperations.Update, SecurityOperations.Delete);
        await fixture.GrantAsync(ownerId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.GrantAsync(readerId, visibleScope, SecurityOperations.Read);
        await fixture.RebuildAccessAsync();

        Guid visibleId;
        Guid hiddenId;
        DateTimeOffset visibleToken;
        using (var owner = fixture.ForActor(ownerId))
        {
            var type = await owner.FacilityTypes.CreateAsync(new(
                "AUTH-" + Guid.NewGuid().ToString("N"), "Authorization", null, true, true, [], typeScope));
            var visible = await owner.Facilities.CreateAsync(new("Visible", null, type.id, null, true, visibleScope, "VIS-01"));
            var hidden = await owner.Facilities.CreateAsync(new("Hidden", null, type.id, null, true, hiddenScope, "HID-01"));
            visibleId = visible.id;
            hiddenId = hidden.id;
            visibleToken = visible.updated_at;
        }

        using (var reader = fixture.ForActor(readerId))
        {
            var rows = await reader.Facilities.ListAsync();
            Assert.Contains(rows, x => x.id == visibleId);
            Assert.DoesNotContain(rows, x => x.id == hiddenId);
            var hidden = await fixture.ReadFacilityAsync(hiddenId);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reader.Facilities.UpdateAsync(new(
                hiddenId, hidden.updated_at, "No access", null, hidden.FacilityTypeId, false, "NO-ACCESS")));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reader.Facilities.DeleteAsync(
                hiddenId, new(hidden.updated_at)));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reader.Facilities.MoveAsync(
                visibleId, new(hiddenId, visibleToken)));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reader.Facilities.CreateAsync(new(
                "No create", null, rows.First(x => x.id == visibleId).FacilityTypeId, null, true, hiddenScope, "NO-CREATE")));
        }

        var unchanged = await fixture.ReadFacilitiesAsync(visibleId, hiddenId);
        Assert.Equal("Visible", unchanged.Single(x => x.id == visibleId).Name);
        Assert.Equal("Hidden", unchanged.Single(x => x.id == hiddenId).Name);
        Assert.Null(unchanged.Single(x => x.id == visibleId).ParentId);
    }

    [Fact]
    public async Task TypedAndGenericFacilityUpdatesRejectStaleClientTokensWithoutChangingPersistedData()
    {
        var actorId = await fixture.CreateActorAsync("Stale token updater");
        var facilityScope = await fixture.CreateScopeAsync("Stale token facility scope");
        var typeScope = await fixture.CreateScopeAsync("Stale token type scope");
        await fixture.GrantAsync(actorId, facilityScope, SecurityOperations.Read, SecurityOperations.Create,
            SecurityOperations.Update);
        await fixture.GrantAsync(actorId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        Guid facilityId;
        FacilityResponse original;
        using (var actor = fixture.ForActor(actorId))
        {
            var type = await actor.FacilityTypes.CreateAsync(new(
                "STALE-" + Guid.NewGuid().ToString("N"), "Stale token", null, true, true, [], typeScope));
            original = await actor.Facilities.CreateAsync(new(
                "Original", null, type.id, null, true, facilityScope, "STALE-01"));
            facilityId = original.id;
            var current = await actor.Facilities.UpdateAsync(new(
                original.id, original.updated_at, "Current", null, type.id, true, "STALE-02"));
            Assert.True(current.updated_at > original.updated_at);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => actor.Facilities.UpdateAsync(new(
                original.id, original.updated_at, "Stale typed write", null, type.id, true, "STALE-03")));
            ISecuredEntityService<FacilityModel> genericService = actor.Facilities;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                genericService.UpdateAsync<FacilityUpdateRequest, FacilityResponse>(new(
                    original.id, original.updated_at, "Stale generic write", null, type.id, true, "STALE-04")));
        }

        var persisted = await fixture.ReadFacilityAsync(facilityId);
        Assert.Equal("Current", persisted.Name);
        Assert.Equal("STALE-02", persisted.Code);
        Assert.Equal(actorId, persisted.updated_by);
    }

    [Fact]
    public async Task MoveRejectsStaleClientTokenBeforeCopyingScopesOrChangingPaths()
    {
        var updaterId = await fixture.CreateActorAsync("Stale move updater");
        var moverId = await fixture.CreateActorAsync("Stale move permission holder");
        var sourceScope = await fixture.CreateScopeAsync("Stale move source");
        var destinationScope = await fixture.CreateScopeAsync("Stale move destination");
        var typeScope = await fixture.CreateScopeAsync("Stale move types");
        await fixture.GrantAsync(updaterId, sourceScope, SecurityOperations.Read, SecurityOperations.Create,
            SecurityOperations.Update);
        await fixture.GrantAsync(updaterId, destinationScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.GrantAsync(updaterId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        Guid sourceId;
        Guid destinationId;
        FacilityResponse source;
        using (var updater = fixture.ForActor(updaterId))
        {
            var type = await updater.FacilityTypes.CreateAsync(new(
                "STALE-MOVE-" + Guid.NewGuid().ToString("N"), "Stale move", null, true, true, [], typeScope));
            source = await updater.Facilities.CreateAsync(new(
                "Move source", null, type.id, null, true, sourceScope, "STALE-MOVE-01"));
            var destination = await updater.Facilities.CreateAsync(new(
                "Move destination", null, type.id, null, true, destinationScope, "STALE-MOVE-02"));
            sourceId = source.id;
            destinationId = destination.id;
            await updater.Facilities.UpdateAsync(new(
                source.id, source.updated_at, "Updated source", null, type.id, true, "STALE-MOVE-03"));
        }

        var sourceAfterUpdate = await fixture.ReadFacilityAsync(sourceId);
        await fixture.GrantMoveAsync(moverId, sourceAfterUpdate.rbacobject_id);
        await fixture.GrantAsync(moverId, (await fixture.ReadFacilityAsync(destinationId)).rbacobject_id,
            SecurityOperations.Create);
        await fixture.RebuildAccessAsync();
        Assert.True(await fixture.IsAllowedAsync(moverId, sourceAfterUpdate.rbacobject_id,
            FacilityPermissionTree.MoveOperation));
        Assert.False(await fixture.IsAllowedAsync(moverId, sourceAfterUpdate.rbacobject_id,
            SecurityOperations.Update));
        var groupsBeforeStaleMove = await fixture.CountGroupsAsync();
        using (var mover = fixture.ForActor(moverId))
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => mover.Facilities.MoveAsync(
                sourceId, new(destinationId, source.updated_at)));
        Assert.Equal(groupsBeforeStaleMove, await fixture.CountGroupsAsync());

        var unchanged = await fixture.ReadFacilityAsync(sourceId);
        Assert.Null(unchanged.ParentId);
        Assert.Equal($"/{sourceId:D}/", unchanged.path);
        Assert.Equal(source.rbacobject_id, unchanged.rbacobject_id);
        Assert.Equal("Updated source", unchanged.Name);
        Assert.NotEqual(destinationId, unchanged.ParentId);
    }

    [Fact]
    public async Task MoveCopiesPermissionSubtreeRebindsFacilitiesAndPreservesSourcePermissions()
    {
        var ownerId = await fixture.CreateActorAsync("Permission-tree mover");
        var copiedReaderId = await fixture.CreateActorAsync("Copied role reader");
        var oldAncestorReaderId = await fixture.CreateActorAsync("Source ancestor-only reader");
        var inheritedReaderId = await fixture.CreateActorAsync("Destination inherited reader");
        var deniedReaderId = await fixture.CreateActorAsync("Denied subtree reader");
        var sourceRequestScope = await fixture.CreateScopeAsync("Move source request scope");
        var destinationRequestScope = await fixture.CreateScopeAsync("Move destination request scope");
        var typeScope = await fixture.CreateScopeAsync("Move facility type scope");
        await fixture.GrantAsync(ownerId, sourceRequestScope, SecurityOperations.Create);
        await fixture.GrantAsync(ownerId, destinationRequestScope, SecurityOperations.Create);
        await fixture.GrantAsync(ownerId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        Guid sourceId;
        Guid childId;
        Guid destinationId;
        FacilityResponse source;
        FacilityResponse child;
        FacilityResponse destination;
        using (var owner = fixture.ForActor(ownerId))
        {
            var type = await owner.FacilityTypes.CreateAsync(new(
                "MOVE-" + Guid.NewGuid().ToString("N"), "Movable", null, true, true, [], typeScope));
            source = await owner.Facilities.CreateAsync(new(
                "Move source", null, type.id, null, true, sourceRequestScope, "MOVE-SOURCE"));
            child = await owner.Facilities.CreateAsync(new(
                "Move child", null, type.id, source.id, true, sourceRequestScope, "MOVE-CHILD"));
            destination = await owner.Facilities.CreateAsync(new(
                "Move destination", null, type.id, null, true, destinationRequestScope, "MOVE-DESTINATION"));
            sourceId = source.id;
            childId = child.id;
            destinationId = destination.id;
        }

        var readRoleType = await fixture.CreateRoleTypeAsync(
            "Copied facility reader " + Guid.NewGuid().ToString("N"), SecurityOperations.Read);
        var deniedRoleType = await fixture.CreateRoleTypeAsync(
            "Denied facility reader " + Guid.NewGuid().ToString("N"), SecurityOperations.Read);
        var inheritedRoleType = await fixture.CreateRoleTypeAsync(
            "Destination inherited reader " + Guid.NewGuid().ToString("N"), SecurityOperations.Read);
        var sourceRoleId = await fixture.AssignRoleAsync(readRoleType, copiedReaderId, source.rbacobject_id);
        var oldAncestorRoleId = await fixture.AssignRoleAsync(
            readRoleType, oldAncestorReaderId, sourceRequestScope);
        var deniedRoleId = await fixture.AssignRoleAsync(deniedRoleType, deniedReaderId, child.rbacobject_id);
        var inheritedRoleId = await fixture.AssignRoleAsync(
            inheritedRoleType, inheritedReaderId, destination.rbacobject_id);
        var membershipUserId = await fixture.CreateActorAsync("Copied membership user");
        var sourceMembershipId = await fixture.AddMembershipAsync(membershipUserId, child.rbacobject_id);
        var sourceDenyId = await fixture.AddDenyRuleAsync(deniedRoleType, child.rbacobject_id);
        Assert.False(await fixture.IsAllowedAsync(ownerId, source.rbacobject_id, FacilityPermissionTree.MoveOperation));
        var moveRoleTypeId = await fixture.GrantMoveAsync(ownerId, source.rbacobject_id);
        await fixture.RebuildAccessAsync();
        Assert.True(await fixture.IsAllowedAsync(ownerId, source.rbacobject_id, FacilityPermissionTree.MoveOperation));

        await using (var context = fixture.CreateContext())
            await context.Groups.Where(x => x.id == source.rbacobject_id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.inherit_roles, false));

        await fixture.GrantAsync(ownerId, destination.rbacobject_id, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();
        Assert.True(await fixture.IsAllowedAsync(copiedReaderId, source.rbacobject_id, SecurityOperations.Read));
        Assert.False(await fixture.IsAllowedAsync(deniedReaderId, child.rbacobject_id, SecurityOperations.Read));
        Assert.True(await fixture.IsAllowedAsync(inheritedReaderId, destination.rbacobject_id, SecurityOperations.Read));
        Assert.False(await fixture.IsAllowedAsync(oldAncestorReaderId, source.rbacobject_id, SecurityOperations.Read));

        GroupModel[] originalGroups;
        RoleModel[] originalRoles;
        MembershipModel[] originalMemberships;
        RbacDenyRuleModel[] originalDenies;
        await using (var context = fixture.CreateContext())
        {
            var sourceGroupPath = await context.Groups.AsNoTracking()
                .Where(x => x.id == source.rbacobject_id).Select(x => x.path).SingleAsync();
            originalGroups = await context.Groups.AsNoTracking()
                .Where(x => x.path.StartsWith(sourceGroupPath))
                .OrderBy(x => x.path).ToArrayAsync();
            var originalGroupIds = originalGroups.Select(x => x.id).ToArray();
            originalRoles = await context.Roles.AsNoTracking().Where(x => originalGroupIds.Contains(x.group_id))
                .OrderBy(x => x.id).ToArrayAsync();
            originalMemberships = await context.Memberships.AsNoTracking()
                .Where(x => originalGroupIds.Contains(x.group_id)).OrderBy(x => x.id).ToArrayAsync();
            originalDenies = await context.RbacDenyRules.AsNoTracking()
                .Where(x => originalGroupIds.Contains(x.group_id)).OrderBy(x => x.id).ToArrayAsync();
        }
        Assert.Contains(originalRoles, x => x.id == sourceRoleId);
        Assert.Contains(originalRoles, x => x.id == deniedRoleId);
        Assert.Contains(originalRoles, x => x.role_type_id == moveRoleTypeId);
        Assert.Contains(originalMemberships, x => x.id == sourceMembershipId);
        Assert.Contains(originalDenies, x => x.id == sourceDenyId);

        using (var owner = fixture.ForActor(ownerId))
        {
            Assert.True(await fixture.IsAllowedAsync(ownerId, source.rbacobject_id,
                FacilityPermissionTree.MoveOperation));
            Assert.False(await fixture.IsAllowedAsync(ownerId, source.rbacobject_id, SecurityOperations.Update));
            Assert.False(await fixture.IsAllowedAsync(ownerId, child.rbacobject_id, SecurityOperations.Update));
            Assert.True(await fixture.IsAllowedAsync(ownerId, destination.rbacobject_id, SecurityOperations.Create));
            var moved = await owner.Facilities.MoveAsync(sourceId, new(destinationId, source.updated_at));
            Assert.Equal(destinationId, moved.ParentId);
        }

        var movedRoot = await fixture.ReadFacilityAsync(sourceId);
        var movedChild = await fixture.ReadFacilityAsync(childId);
        Assert.Equal($"{destination.path}{sourceId:D}/", movedRoot.path);
        Assert.Equal($"{movedRoot.path}{childId:D}/", movedChild.path);
        Assert.NotEqual(source.rbacobject_id, movedRoot.rbacobject_id);
        Assert.NotEqual(child.rbacobject_id, movedChild.rbacobject_id);

        await using (var context = fixture.CreateContext())
        {
            var movedGroupPath = await context.Groups.AsNoTracking()
                .Where(x => x.id == movedRoot.rbacobject_id).Select(x => x.path).SingleAsync();
            var copiedGroups = await context.Groups.AsNoTracking()
                .Where(x => x.path.StartsWith(movedGroupPath))
                .OrderBy(x => x.path).ToArrayAsync();
            Assert.Equal(2, copiedGroups.Length);
            Assert.False(originalGroups.Single(x => x.id == source.rbacobject_id).inherit_roles);
            Assert.Equal(destination.rbacobject_id, copiedGroups[0].master_group_id);
            Assert.True(copiedGroups[0].inherit_roles);
            Assert.Equal(movedRoot.rbacobject_id, copiedGroups[1].master_group_id);
            Assert.Equal(copiedGroups[0].id, movedRoot.rbacobject_id);
            Assert.Equal(copiedGroups[1].id, movedChild.rbacobject_id);
            Assert.All(originalGroups, original =>
                Assert.Equal(original, context.Groups.AsNoTracking().Single(x => x.id == original.id)));

            var groupMap = originalGroups.Zip(copiedGroups)
                .ToDictionary(pair => pair.First.id, pair => pair.Second.id);
            foreach (var original in originalGroups)
            {
                var copy = copiedGroups.Single(x => x.id == groupMap[original.id]);
                Assert.Equal(original.Name, copy.Name);
                Assert.Equal(original.group_type_id, copy.group_type_id);
                Assert.Equal(original.valid, copy.valid);
                Assert.Equal(original.id == source.rbacobject_id || original.inherit_roles, copy.inherit_roles);
            }

            var newGroupIds = copiedGroups.Select(x => x.id).ToArray();
            var copiedRoles = await context.Roles.AsNoTracking()
                .Where(x => newGroupIds.Contains(x.group_id)).ToArrayAsync();
            var copiedMemberships = await context.Memberships.AsNoTracking()
                .Where(x => newGroupIds.Contains(x.group_id)).ToArrayAsync();
            var copiedDenies = await context.RbacDenyRules.AsNoTracking()
                .Where(x => newGroupIds.Contains(x.group_id)).ToArrayAsync();
            Assert.Equal(originalRoles.Length, copiedRoles.Length);
            Assert.Equal(originalMemberships.Length, copiedMemberships.Length);
            Assert.Equal(originalDenies.Length, copiedDenies.Length);
            Assert.All(originalRoles, original => Assert.Contains(copiedRoles, copy =>
                copy.group_id == groupMap[original.group_id] && copy.id != original.id &&
                copy.user_id == original.user_id && copy.role_type_id == original.role_type_id &&
                copy.valid_from == original.valid_from && copy.valid_to == original.valid_to));
            Assert.All(originalMemberships, original => Assert.Contains(copiedMemberships, copy =>
                copy.group_id == groupMap[original.group_id] && copy.id != original.id &&
                copy.user_id == original.user_id && copy.valid_from == original.valid_from &&
                copy.valid_to == original.valid_to));
            Assert.All(originalDenies, original => Assert.Contains(copiedDenies, copy =>
                copy.group_id == groupMap[original.group_id] && copy.id != original.id &&
                copy.role_type_id == original.role_type_id &&
                copy.include_descendants == original.include_descendants));
            Assert.True(await context.Roles.AnyAsync(x =>
                x.id == inheritedRoleId && x.group_id == destination.rbacobject_id));
            Assert.All(originalRoles, original =>
                Assert.Equal(original, context.Roles.AsNoTracking().Single(x => x.id == original.id)));
            Assert.All(originalMemberships, original =>
                Assert.Equal(original, context.Memberships.AsNoTracking().Single(x => x.id == original.id)));
            Assert.All(originalDenies, original =>
                Assert.Equal(original, context.RbacDenyRules.AsNoTracking().Single(x => x.id == original.id)));
            Assert.True(await context.Roles.AnyAsync(x =>
                x.id == oldAncestorRoleId && x.group_id == sourceRequestScope));
        }

        Assert.True(await fixture.IsAllowedAsync(copiedReaderId, source.rbacobject_id, SecurityOperations.Read));
        Assert.True(await fixture.IsAllowedAsync(copiedReaderId, movedRoot.rbacobject_id, SecurityOperations.Read));
        Assert.True(await fixture.IsAllowedAsync(copiedReaderId, movedChild.rbacobject_id, SecurityOperations.Read));
        Assert.True(await fixture.IsAllowedAsync(inheritedReaderId, movedRoot.rbacobject_id, SecurityOperations.Read));
        Assert.True(await fixture.IsAllowedAsync(inheritedReaderId, movedChild.rbacobject_id, SecurityOperations.Read));
        Assert.False(await fixture.IsAllowedAsync(oldAncestorReaderId, movedRoot.rbacobject_id, SecurityOperations.Read));
        Assert.False(await fixture.IsAllowedAsync(oldAncestorReaderId, movedChild.rbacobject_id, SecurityOperations.Read));
        Assert.False(await fixture.IsAllowedAsync(inheritedReaderId, source.rbacobject_id, SecurityOperations.Read));
        Assert.True(await fixture.IsAllowedAsync(oldAncestorReaderId, sourceRequestScope, SecurityOperations.Read));
        Assert.False(await fixture.IsAllowedAsync(deniedReaderId, child.rbacobject_id, SecurityOperations.Read));
        Assert.False(await fixture.IsAllowedAsync(deniedReaderId, movedChild.rbacobject_id, SecurityOperations.Read));
    }

    [Fact]
    public async Task StaleMoveWaitsForTreeTransactionLockBeforeValidatingClientToken()
    {
        var updaterId = await fixture.CreateActorAsync("Serialized move updater");
        var moverId = await fixture.CreateActorAsync("Serialized move permission holder");
        var sourceScope = await fixture.CreateScopeAsync("Serialized stale source");
        var destinationScope = await fixture.CreateScopeAsync("Serialized stale destination");
        var typeScope = await fixture.CreateScopeAsync("Serialized stale types");
        await fixture.GrantAsync(updaterId, sourceScope, SecurityOperations.Read, SecurityOperations.Create,
            SecurityOperations.Update);
        await fixture.GrantAsync(updaterId, destinationScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.GrantAsync(updaterId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        Guid sourceId;
        Guid destinationId;
        DateTimeOffset staleToken;
        using (var setup = fixture.ForActor(updaterId))
        {
            var type = await setup.FacilityTypes.CreateAsync(new(
                "SERIAL-" + Guid.NewGuid().ToString("N"), "Serialized move", null, true, true, [], typeScope));
            var source = await setup.Facilities.CreateAsync(new(
                "Serialized source", null, type.id, null, true, sourceScope, "SERIAL-SRC"));
            var destination = await setup.Facilities.CreateAsync(new(
                "Serialized destination", null, type.id, null, true, destinationScope, "SERIAL-DST"));
            sourceId = source.id;
            destinationId = destination.id;
            await setup.Facilities.UpdateAsync(new(
                source.id, source.updated_at, "Serialized source updated", null, type.id, true, "SERIAL-NEW"));
            staleToken = source.updated_at;
        }

        var sourceAfterUpdate = await fixture.ReadFacilityAsync(sourceId);
        await fixture.GrantMoveAsync(moverId, sourceAfterUpdate.rbacobject_id);
        await fixture.GrantAsync(moverId, (await fixture.ReadFacilityAsync(destinationId)).rbacobject_id,
            SecurityOperations.Create);
        await fixture.RebuildAccessAsync();
        Assert.False(await fixture.IsAllowedAsync(moverId, sourceAfterUpdate.rbacobject_id,
            SecurityOperations.Update));

        await using var blocker = fixture.CreateContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(762306241)");
        using var mover = fixture.ForActor(moverId);
        var move = mover.Facilities.MoveAsync(sourceId, new(destinationId, staleToken));

        var completedBeforeUnlock = await Task.WhenAny(move, Task.Delay(TimeSpan.FromMilliseconds(250)));
        Assert.NotSame(move, completedBeforeUnlock);
        await transaction.CommitAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => move);
        var sourceAfter = await fixture.ReadFacilityAsync(sourceId);
        Assert.Null(sourceAfter.ParentId);
        Assert.Equal($"/{sourceId:D}/", sourceAfter.path);
        Assert.Equal("Serialized source updated", sourceAfter.Name);
    }

    [Fact]
    public async Task UnauthorizedDestinationCannotMoveOrChangeSourcePaths()
    {
        var actorId = await fixture.CreateActorAsync("Restricted physical mover");
        var sourceParentScope = await fixture.CreateScopeAsync("Restricted mover source parent");
        var destinationParentScope = await fixture.CreateScopeAsync("Restricted mover destination parent");
        var typeScope = await fixture.CreateScopeAsync("Restricted mover types");
        await fixture.GrantAsync(actorId, sourceParentScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.GrantAsync(actorId, destinationParentScope, SecurityOperations.Read);
        await fixture.GrantAsync(actorId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        Guid sourceId;
        Guid destinationId;
        using (var actor = fixture.ForActor(actorId))
        {
            var type = await actor.FacilityTypes.CreateAsync(new(
                "RESTRICTED-MOVE-" + Guid.NewGuid().ToString("N"), "Restricted move", null, true, true, [], typeScope));
            var source = await actor.Facilities.CreateAsync(new(
                "Source", null, type.id, null, true, sourceParentScope, "RESTRICTED-SRC"));
            destinationId = await fixture.SeedFacilityAsync(
                destinationParentScope, type.id, "Unauthorized destination");
            sourceId = source.id;
            await fixture.GrantAsync(actorId, source.rbacobject_id, FacilityPermissionTree.MoveOperation);
            await fixture.RebuildAccessAsync();

            var beforeDeniedDestination = await fixture.ReadFacilitySubtreeAsync(source.path);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => actor.Facilities.MoveAsync(
                source.id, new(destinationId, source.updated_at)));
            Assert.Equal(beforeDeniedDestination.ToDictionary(x => x.id, x => (x.path, x.rbacobject_id)),
                (await fixture.ReadFacilitySubtreeAsync(source.path)).ToDictionary(x => x.id, x => (x.path, x.rbacobject_id)));
        }
    }

    [Fact]
    public async Task MoveRejectsFacilityScopeOutsidePhysicalSubtreeWithoutChangingAnything()
    {
        var actorId = await fixture.CreateActorAsync("Orphan permission scope mover");
        var requestScope = await fixture.CreateScopeAsync("Orphan scope move request parent");
        var typeScope = await fixture.CreateScopeAsync("Orphan scope move types");
        await fixture.GrantAsync(actorId, requestScope, SecurityOperations.Create);
        await fixture.GrantAsync(actorId, typeScope, SecurityOperations.Read, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        Guid sourceId;
        Guid destinationId;
        Guid orphanFacilityId;
        Guid orphanScopeId;
        GroupModel orphanGroup;
        FacilityResponse source;
        using (var actor = fixture.ForActor(actorId))
        {
            var type = await actor.FacilityTypes.CreateAsync(new(
                "ORPHAN-MOVE-" + Guid.NewGuid().ToString("N"), "Orphan move scope", null, true, true, [], typeScope));
            source = await actor.Facilities.CreateAsync(new(
                "Orphan move source", null, type.id, null, true, requestScope, "ORPHAN-SOURCE"));
            var destination = await actor.Facilities.CreateAsync(new(
                "Orphan move destination", null, type.id, null, true, requestScope, "ORPHAN-DESTINATION"));
            sourceId = source.id;
            destinationId = destination.id;

            var sourceGroup = (await fixture.ReadGroupsAsync(source.rbacobject_id)).Single();
            orphanScopeId = Guid.NewGuid();
            var now = fixture.Now;
            await using (var context = fixture.CreateContext())
            {
                orphanGroup = new GroupModel(orphanScopeId, "Detached physical facility scope",
                    sourceGroup.group_type_id, sourceGroup.id, $"{sourceGroup.path}{orphanScopeId:D}/",
                    true, orphanScopeId, now, now, fixture.AdministratorId, fixture.AdministratorId);
                context.Groups.Add(orphanGroup);
                await context.SaveChangesAsync();
            }
            orphanFacilityId = await fixture.SeedFacilityAsync(orphanScopeId, type.id, "Outside physical source subtree");
            await fixture.GrantMoveAsync(actorId, source.rbacobject_id);
            await fixture.GrantAsync(actorId, destination.rbacobject_id, SecurityOperations.Create);
            await fixture.RebuildAccessAsync();
        }

        var sourceBefore = await fixture.ReadFacilityAsync(sourceId);
        var orphanBefore = await fixture.ReadFacilityAsync(orphanFacilityId);
        var groupsBefore = await fixture.ReadGroupsAsync(source.rbacobject_id, orphanScopeId);
        var totalGroupsBefore = await fixture.CountGroupsAsync();
        using (var actor = fixture.ForActor(actorId))
            await Assert.ThrowsAsync<InvalidOperationException>(() => actor.Facilities.MoveAsync(
                sourceId, new(destinationId, sourceBefore.updated_at)));

        var sourceAfter = await fixture.ReadFacilityAsync(sourceId);
        var orphanAfter = await fixture.ReadFacilityAsync(orphanFacilityId);
        Assert.Equal(sourceBefore.path, sourceAfter.path);
        Assert.Equal(sourceBefore.rbacobject_id, sourceAfter.rbacobject_id);
        Assert.Equal(sourceBefore.ParentId, sourceAfter.ParentId);
        Assert.Equal(orphanBefore.path, orphanAfter.path);
        Assert.Equal(orphanBefore.rbacobject_id, orphanAfter.rbacobject_id);
        Assert.Null(orphanAfter.ParentId);
        var groupsAfter = await fixture.ReadGroupsAsync(groupsBefore.Select(x => x.id).ToArray());
        Assert.Equal(groupsBefore.Select(x => x.id).OrderBy(x => x),
            groupsAfter.Select(x => x.id).OrderBy(x => x));
        var orphanGroupAfter = (await fixture.ReadGroupsAsync(orphanScopeId)).Single();
        Assert.Equal(orphanGroup.id, orphanGroupAfter.id);
        Assert.Equal(orphanGroup.Name, orphanGroupAfter.Name);
        Assert.Equal(orphanGroup.master_group_id, orphanGroupAfter.master_group_id);
        Assert.Equal(orphanGroup.path, orphanGroupAfter.path);
        Assert.Equal(orphanGroup.inherit_roles, orphanGroupAfter.inherit_roles);
        Assert.Equal(orphanGroup.rbacobject_id, orphanGroupAfter.rbacobject_id);
        Assert.Equal(orphanGroup.valid, orphanGroupAfter.valid);
        Assert.Equal(totalGroupsBefore, await fixture.CountGroupsAsync());
    }

    [Fact]
    public async Task ReservationRequestRequiresFacilityReadEventUpdateAndExplicitReservationCreateScope()
    {
        var scenario = await CreateReservationScenario();
        var requester = await fixture.CreateActorAsync("Reservation requester");
        await fixture.GrantAsync(requester, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(requester, scenario.EventScope, SecurityOperations.Update);
        await fixture.GrantAsync(requester, scenario.ReservationScope, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        using var actor = fixture.ForActor(requester);
        var created = await actor.Reservations.RequestAsync(new(
            scenario.FacilityId, scenario.EventId, scenario.Start, scenario.Start.AddHours(1),
            scenario.ReservationScope, "requested"));
        Assert.Equal(ReservationStatuses.Requested, created.Status);
        Assert.Equal(scenario.ReservationScope, created.rbacobject_id);
        Assert.Equal(requester, created.created_by);
        Assert.Equal(requester, created.updated_by);
        Assert.Equal(scenario.EventId, created.EventId);

        var readOnlyEventActor = await fixture.CreateActorAsync("Event reader without update");
        await fixture.GrantAsync(readOnlyEventActor, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(readOnlyEventActor, scenario.EventScope, SecurityOperations.Read);
        await fixture.GrantAsync(readOnlyEventActor, scenario.ReservationScope, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();
        using var denied = fixture.ForActor(readOnlyEventActor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => denied.Reservations.RequestAsync(new(
            scenario.FacilityId, scenario.EventId, scenario.Start.AddHours(2), scenario.Start.AddHours(3),
            scenario.ReservationScope)));

        var implicitScopeActor = await fixture.CreateActorAsync("Facility create is not reservation create");
        await fixture.GrantAsync(implicitScopeActor, scenario.FacilityScope, SecurityOperations.Read,
            SecurityOperations.Create);
        await fixture.GrantAsync(implicitScopeActor, scenario.EventScope, SecurityOperations.Update);
        await fixture.RebuildAccessAsync();
        using var wrongScope = fixture.ForActor(implicitScopeActor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => wrongScope.Reservations.RequestAsync(new(
            scenario.FacilityId, scenario.EventId, scenario.Start.AddHours(4), scenario.Start.AddHours(5),
            scenario.ReservationScope)));
    }

    [Fact]
    public async Task ReservationTransitionsRequireUpdateOnFacilityEventAndReservation()
    {
        var scenario = await CreateReservationScenario();
        var fullyAuthorized = await fixture.CreateActorAsync("Reservation approver");
        await fixture.GrantAsync(fullyAuthorized, scenario.FacilityScope, SecurityOperations.Update);
        await fixture.GrantAsync(fullyAuthorized, scenario.EventScope, SecurityOperations.Update);
        await fixture.GrantAsync(fullyAuthorized, scenario.ReservationScope, SecurityOperations.Update);
        var missingFacilityUpdate = await fixture.CreateActorAsync("Facility read-only approver");
        await fixture.GrantAsync(missingFacilityUpdate, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingFacilityUpdate, scenario.EventScope, SecurityOperations.Update);
        await fixture.GrantAsync(missingFacilityUpdate, scenario.ReservationScope, SecurityOperations.Update);
        var missingEventUpdate = await fixture.CreateActorAsync("Event read-only approver");
        await fixture.GrantAsync(missingEventUpdate, scenario.FacilityScope, SecurityOperations.Read, SecurityOperations.Update);
        await fixture.GrantAsync(missingEventUpdate, scenario.EventScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingEventUpdate, scenario.ReservationScope, SecurityOperations.Update);
        var missingReservationUpdate = await fixture.CreateActorAsync("Reservation read-only approver");
        await fixture.GrantAsync(missingReservationUpdate, scenario.FacilityScope, SecurityOperations.Read, SecurityOperations.Update);
        await fixture.GrantAsync(missingReservationUpdate, scenario.EventScope, SecurityOperations.Update);
        await fixture.RebuildAccessAsync();
        foreach (var scope in new[] { scenario.FacilityScope, scenario.EventScope, scenario.ReservationScope })
            Assert.True(await fixture.IsAllowedAsync(fullyAuthorized, scope, SecurityOperations.Update));
        foreach (var scope in new[] { scenario.FacilityScope, scenario.EventScope, scenario.ReservationScope })
            Assert.False(await fixture.IsAllowedAsync(fullyAuthorized, scope, SecurityOperations.Read));

        var reservationIds = new[]
        {
            await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
                scenario.ReservationScope, scenario.Start, scenario.Start.AddHours(1)),
            await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
                scenario.ReservationScope, scenario.Start.AddHours(2), scenario.Start.AddHours(3)),
            await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
                scenario.ReservationScope, scenario.Start.AddHours(4), scenario.Start.AddHours(5))
        };
        var tokens = await Task.WhenAll(reservationIds.Select(async id =>
            (await fixture.ReadReservationAsync(id)).updated_at));
        using (var denied = fixture.ForActor(missingFacilityUpdate))
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => denied.Reservations.ConfirmAsync(
                reservationIds[0], new(tokens[0])));
        using (var denied = fixture.ForActor(missingEventUpdate))
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => denied.Reservations.ConfirmAsync(
                reservationIds[1], new(tokens[1])));
        using (var denied = fixture.ForActor(missingReservationUpdate))
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => denied.Reservations.ConfirmAsync(
                reservationIds[2], new(tokens[2])));

        using var approver = fixture.ForActor(fullyAuthorized);
        var confirmed = await approver.Reservations.ConfirmAsync(reservationIds[0], new(tokens[0], "approved"));
        Assert.Equal(ReservationStatuses.Confirmed, confirmed.Status);
        Assert.Equal(fullyAuthorized, confirmed.updated_by);
        Assert.Equal("approved", confirmed.DecisionReason);
        Assert.Equal(reservationIds[0], confirmed.id);
        Assert.All(await Task.WhenAll(reservationIds.Skip(1).Select(fixture.ReadReservationAsync)),
            item => Assert.Equal(ReservationStatuses.Requested, item.Status));
    }

    [Fact]
    public async Task ReservationGenericVectorPageAndWhereWithUpdateStillRequireFacilityAndEventRead()
    {
        var scenario = await CreateReservationScenario();
        var reservationId = await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
            scenario.ReservationScope, scenario.Start, scenario.Start.AddHours(1));
        var readerId = await fixture.CreateActorAsync("Reservation vector reader without related read");
        await fixture.GrantAsync(readerId, scenario.ReservationScope,
            SecurityOperations.Read, SecurityOperations.Update);
        var facilityReaderId = await fixture.CreateActorAsync("Facility-readable reservation vector reader");
        await fixture.GrantAsync(facilityReaderId, scenario.ReservationScope,
            SecurityOperations.Read, SecurityOperations.Update);
        await fixture.GrantAsync(facilityReaderId, scenario.FacilityScope, SecurityOperations.Read);
        var eventReaderId = await fixture.CreateActorAsync("Event-readable reservation vector reader");
        await fixture.GrantAsync(eventReaderId, scenario.ReservationScope,
            SecurityOperations.Read, SecurityOperations.Update);
        await fixture.GrantAsync(eventReaderId, scenario.EventScope, SecurityOperations.Read);
        var relatedReaderId = await fixture.CreateActorAsync("Related-readable reservation vector reader");
        await fixture.GrantAsync(relatedReaderId, scenario.ReservationScope,
            SecurityOperations.Read, SecurityOperations.Update);
        await fixture.GrantAsync(relatedReaderId, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(relatedReaderId, scenario.EventScope, SecurityOperations.Read);
        var missingReservationReadId = await fixture.CreateActorAsync("Reservation update without reservation read");
        await fixture.GrantAsync(missingReservationReadId, scenario.ReservationScope, SecurityOperations.Update);
        await fixture.GrantAsync(missingReservationReadId, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingReservationReadId, scenario.EventScope, SecurityOperations.Read);
        await fixture.RebuildAccessAsync();

        async Task AssertUpdateQueriesAreEmpty(Guid actorId)
        {
            using var reader = fixture.ForActor(actorId);
            Assert.Empty(await reader.Reservations.VectorAsync<ReservationResponse>(
                x => x.id == reservationId, SecurityOperations.Update));
            Assert.Empty((await reader.Reservations.VectorPageAsync<ReservationResponse>(
                x => x.id == reservationId, SecurityOperations.Update, new(Take: 10))).Items);
            Assert.Empty(reader.Reservations.Where<ReservationResponse>(
                x => x.id == reservationId, SecurityOperations.Update).ToArray());
        }

        await AssertUpdateQueriesAreEmpty(readerId);
        await AssertUpdateQueriesAreEmpty(facilityReaderId);
        await AssertUpdateQueriesAreEmpty(eventReaderId);
        await AssertUpdateQueriesAreEmpty(missingReservationReadId);

        using var fullyRelatedReader = fixture.ForActor(relatedReaderId);
        Assert.Contains(await fullyRelatedReader.Reservations.VectorAsync<ReservationResponse>(
            x => x.id == reservationId, SecurityOperations.Update), x => x.id == reservationId);
        Assert.Contains((await fullyRelatedReader.Reservations.VectorPageAsync<ReservationResponse>(
            x => x.id == reservationId, SecurityOperations.Update, new(Take: 10))).Items,
            x => x.id == reservationId);
        Assert.Contains(fullyRelatedReader.Reservations.Where<ReservationResponse>(
            x => x.id == reservationId, SecurityOperations.Update), x => x.id == reservationId);
    }

    [Fact]
    public async Task FutureConfirmedReservationRejectsNonreservableTypeReassignmentAtomically()
    {
        var scenario = await CreateReservationScenario();
        var nonreservableTypeScope = await fixture.CreateScopeAsync("Nonreservable replacement type scope");
        var nonreservableTypeId = await fixture.SeedFacilityTypeAsync(nonreservableTypeScope, reservable: false);
        var updater = await fixture.CreateActorAsync("Facility type reassignment without booking cancellation");
        await fixture.GrantAsync(updater, scenario.FacilityScope, SecurityOperations.Update);
        await fixture.GrantAsync(updater, scenario.ReservationScope, SecurityOperations.Read);
        await fixture.GrantAsync(updater, nonreservableTypeScope, SecurityOperations.Read);
        await fixture.RebuildAccessAsync();

        var reservationId = await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
            scenario.ReservationScope, scenario.Start, scenario.Start.AddHours(1),
            ReservationStatuses.Confirmed);
        var before = await fixture.ReadFacilityAsync(scenario.FacilityId);
        Assert.True(before.IsActive);

        using var actor = fixture.ForActor(updater);
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.Facilities.UpdateAsync(new(
            before.id, before.updated_at, before.Name, before.Description, nonreservableTypeId,
            before.IsActive, before.Code, before.Capacity)));

        var after = await fixture.ReadFacilityAsync(scenario.FacilityId);
        Assert.Equal(before.id, after.id);
        Assert.Equal(before.FacilityTypeId, after.FacilityTypeId);
        Assert.Equal(before.Code, after.Code);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.Description, after.Description);
        Assert.Equal(before.IsActive, after.IsActive);
        Assert.Equal(before.rbacobject_id, after.rbacobject_id);
        Assert.Equal(before.created_at, after.created_at);
        Assert.Equal(before.updated_at, after.updated_at);
        Assert.Equal(before.created_by, after.created_by);
        Assert.Equal(before.updated_by, after.updated_by);
        var persistedReservation = await fixture.ReadReservationAsync(reservationId);
        Assert.Equal(ReservationStatuses.Confirmed, persistedReservation.Status);
        Assert.Equal(scenario.FacilityId, persistedReservation.FacilityId);
    }

    [Fact]
    public async Task ReservationReadRequiresReadOnReservationFacilityAndEvent()
    {
        var scenario = await CreateReservationScenario();
        var allReads = await fixture.CreateActorAsync("Reservation reader");
        await fixture.GrantAsync(allReads, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(allReads, scenario.EventScope, SecurityOperations.Read);
        await fixture.GrantAsync(allReads, scenario.ReservationScope, SecurityOperations.Read);
        var missingFacilityRead = await fixture.CreateActorAsync("Missing facility read");
        await fixture.GrantAsync(missingFacilityRead, scenario.EventScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingFacilityRead, scenario.ReservationScope, SecurityOperations.Read);
        var missingEventRead = await fixture.CreateActorAsync("Missing event read");
        await fixture.GrantAsync(missingEventRead, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingEventRead, scenario.ReservationScope, SecurityOperations.Read);
        var missingReservationRead = await fixture.CreateActorAsync("Missing reservation read");
        await fixture.GrantAsync(missingReservationRead, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingReservationRead, scenario.EventScope, SecurityOperations.Read);
        await fixture.RebuildAccessAsync();
        var reservationId = await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
            scenario.ReservationScope, scenario.Start, scenario.Start.AddHours(1), ReservationStatuses.Confirmed);

        using (var reader = fixture.ForActor(allReads))
        {
            Assert.Contains(await reader.Reservations.ListByFacilityAsync(scenario.FacilityId), x => x.id == reservationId);
            Assert.Contains(await reader.Reservations.ListByEventAsync(scenario.EventId), x => x.id == reservationId);
            Assert.Contains(await reader.Reservations.OccupancyAsync(scenario.FacilityId, scenario.Start, scenario.Start.AddHours(2)),
                x => x.id == reservationId);
        }
        using (var reader = fixture.ForActor(missingFacilityRead))
            Assert.Empty(await reader.Reservations.ListByFacilityAsync(scenario.FacilityId));
        using (var reader = fixture.ForActor(missingEventRead))
            Assert.Empty(await reader.Reservations.ListByFacilityAsync(scenario.FacilityId));
        using (var reader = fixture.ForActor(missingReservationRead))
            Assert.Empty(await reader.Reservations.ListByFacilityAsync(scenario.FacilityId));
    }

    [Fact]
    public async Task ListFacilitiesByEventReturnsDistinctHistoryAndRequiresAllThreeReadScopes()
    {
        var scenario = await CreateReservationScenario();
        var secondFacilityScope = await fixture.CreateScopeAsync("Second event facility scope");
        var secondFacilityId = await fixture.SeedFacilityAsync(secondFacilityScope,
            (await fixture.ReadFacilityAsync(scenario.FacilityId)).FacilityTypeId, "Second event facility");
        await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId, scenario.ReservationScope,
            scenario.Start, scenario.Start.AddHours(1), ReservationStatuses.Requested);
        await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId, scenario.ReservationScope,
            scenario.Start.AddHours(2), scenario.Start.AddHours(3), ReservationStatuses.Cancelled);
        await fixture.SeedReservationAsync(secondFacilityId, scenario.EventId, scenario.ReservationScope,
            scenario.Start.AddHours(4), scenario.Start.AddHours(5), ReservationStatuses.Rejected);

        var fullReader = await fixture.CreateActorAsync("Event association reader");
        await fixture.GrantAsync(fullReader, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(fullReader, secondFacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(fullReader, scenario.EventScope, SecurityOperations.Read);
        await fixture.GrantAsync(fullReader, scenario.ReservationScope, SecurityOperations.Read);
        var missingFacilityReader = await fixture.CreateActorAsync("Event association missing facility read");
        await fixture.GrantAsync(missingFacilityReader, scenario.EventScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingFacilityReader, scenario.ReservationScope, SecurityOperations.Read);
        var missingEventReader = await fixture.CreateActorAsync("Event association missing event read");
        await fixture.GrantAsync(missingEventReader, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingEventReader, secondFacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingEventReader, scenario.ReservationScope, SecurityOperations.Read);
        var missingReservationReader = await fixture.CreateActorAsync("Event association missing reservation read");
        await fixture.GrantAsync(missingReservationReader, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingReservationReader, secondFacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(missingReservationReader, scenario.EventScope, SecurityOperations.Read);
        await fixture.RebuildAccessAsync();

        using (var reader = fixture.ForActor(fullReader))
        {
            var facilities = await reader.Reservations.ListFacilitiesByEventAsync(scenario.EventId);
            Assert.Equal(2, facilities.Count);
            Assert.Contains(facilities, x => x.id == scenario.FacilityId);
            Assert.Contains(facilities, x => x.id == secondFacilityId);
            Assert.Single(await reader.Reservations.ListFacilitiesByEventAsync(scenario.EventId, skip: 0, take: 1));
        }

        using (var reader = fixture.ForActor(missingFacilityReader))
            Assert.Empty(await reader.Reservations.ListFacilitiesByEventAsync(scenario.EventId));
        using (var reader = fixture.ForActor(missingEventReader))
            Assert.Empty(await reader.Reservations.ListFacilitiesByEventAsync(scenario.EventId));
        using (var reader = fixture.ForActor(missingReservationReader))
            Assert.Empty(await reader.Reservations.ListFacilitiesByEventAsync(scenario.EventId));
    }

    [Fact]
    public async Task FacilityReadReturnsBooleanAvailabilityWithoutReservationRead()
    {
        var scenario = await CreateReservationScenario();
        var facilityOnlyReader = await fixture.CreateActorAsync("Facility-only availability reader");
        await fixture.GrantAsync(facilityOnlyReader, scenario.FacilityScope, SecurityOperations.Read);
        var fullReader = await fixture.CreateActorAsync("Full availability reader");
        await fixture.GrantAsync(fullReader, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(fullReader, scenario.EventScope, SecurityOperations.Read);
        await fixture.GrantAsync(fullReader, scenario.ReservationScope, SecurityOperations.Read);
        await fixture.RebuildAccessAsync();
        var reservationId = await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
            scenario.ReservationScope, scenario.Start, scenario.Start.AddHours(1), ReservationStatuses.Confirmed);

        using var reader = fixture.ForActor(fullReader);
        var availability = await reader.Reservations.AvailabilityAsync(
            scenario.FacilityId, scenario.Start, scenario.Start.AddHours(1));
        Assert.Equal(scenario.FacilityId, availability.FacilityId);
        Assert.Equal(scenario.Start, availability.StartsAt);
        Assert.Equal(scenario.Start.AddHours(1), availability.EndsAt);
        Assert.False(availability.IsAvailable);
        Assert.Contains(await reader.Reservations.ListByFacilityAsync(scenario.FacilityId),
            reservation => reservation.id == reservationId);

        using var facilityReader = fixture.ForActor(facilityOnlyReader);
        var hiddenBookingAvailability = await facilityReader.Reservations.AvailabilityAsync(
            scenario.FacilityId, scenario.Start, scenario.Start.AddHours(1));
        Assert.Equal(scenario.FacilityId, hiddenBookingAvailability.FacilityId);
        Assert.Equal(scenario.Start, hiddenBookingAvailability.StartsAt);
        Assert.Equal(scenario.Start.AddHours(1), hiddenBookingAvailability.EndsAt);
        Assert.False(hiddenBookingAvailability.IsAvailable);
        Assert.Empty(await facilityReader.Reservations.ListByFacilityAsync(scenario.FacilityId));
        var freeSlotAvailability = await facilityReader.Reservations.AvailabilityAsync(
            scenario.FacilityId, scenario.Start.AddHours(1), scenario.Start.AddHours(2));
        Assert.True(freeSlotAvailability.IsAvailable);
    }

    [Fact]
    public async Task ReservationForeignKeyRestrictsEventDeletion()
    {
        var scenario = await CreateReservationScenario();
        var reservationId = await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
            scenario.ReservationScope, scenario.Start, scenario.Start.AddHours(1));

        await using var context = fixture.CreateContext();
        var eventEntity = await context.Set<EventModel>().SingleAsync(x => x.id == scenario.EventId);
        context.Remove(eventEntity);
        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgresFailure = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal("23001", postgresFailure.SqlState);

        var persisted = await fixture.ReadReservationAsync(reservationId);
        Assert.Equal(scenario.EventId, persisted.EventId);
        await using var verify = fixture.CreateContext();
        Assert.True(await verify.Set<EventModel>().AnyAsync(x => x.id == scenario.EventId));
    }

    [Fact]
    public async Task ConfirmRejectsOverlapsButAllowsAdjacentIntervals()
    {
        var scenario = await CreateReservationScenario();
        var actorId = await GrantReservationOperations(scenario,
            (scenario.FacilityScope, [SecurityOperations.Read, SecurityOperations.Update]),
            (scenario.EventScope, [SecurityOperations.Read, SecurityOperations.Update]),
            (scenario.ReservationScope, [SecurityOperations.Read, SecurityOperations.Create, SecurityOperations.Update]));
        using var actor = fixture.ForActor(actorId);

        var first = await Request(actor, scenario, scenario.Start, scenario.Start.AddHours(1));
        var overlap = await Request(actor, scenario, scenario.Start.AddMinutes(30), scenario.Start.AddHours(1).AddMinutes(30));
        var adjacent = await Request(actor, scenario, scenario.Start.AddHours(1), scenario.Start.AddHours(2));
        await Confirm(actor, first.id, "first");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Confirm(actor, overlap.id, "overlap"));
        var confirmedAdjacent = await Confirm(actor, adjacent.id, "adjacent");

        Assert.Equal(ReservationStatuses.Confirmed, confirmedAdjacent.Status);
        var occupancy = await actor.Reservations.OccupancyAsync(
            scenario.FacilityId, scenario.Start, scenario.Start.AddHours(2));
        Assert.Equal(new[] { first.id, adjacent.id }, occupancy.Select(x => x.id));
    }

    [Fact]
    public async Task ConcurrentConfirmationsForOverlappingRequestsCannotBothCommit()
    {
        var scenario = await CreateReservationScenario();
        var actorId = await GrantReservationOperations(scenario,
            (scenario.FacilityScope, [SecurityOperations.Read, SecurityOperations.Update]),
            (scenario.EventScope, [SecurityOperations.Read, SecurityOperations.Update]),
            (scenario.ReservationScope, [SecurityOperations.Read, SecurityOperations.Create, SecurityOperations.Update]));
        using (var requester = fixture.ForActor(actorId))
        {
            await Request(requester, scenario, scenario.Start, scenario.Start.AddHours(1));
            await Request(requester, scenario, scenario.Start.AddMinutes(15), scenario.Start.AddHours(1).AddMinutes(15));
        }

        using var verify = fixture.ForActor(actorId);
        var reservations = await verify.Reservations.ListByFacilityAsync(scenario.FacilityId);
        var ids = reservations.Select(x => x.id).ToArray();
        Assert.Equal(2, ids.Length);

        async Task<bool> TryConfirm(Guid id)
        {
            using var actor = fixture.ForActor(actorId);
            try
            {
                await Confirm(actor, id, "concurrent");
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        var results = await Task.WhenAll(Task.Run(() => TryConfirm(ids[0])), Task.Run(() => TryConfirm(ids[1])));
        Assert.Single(results, x => x);
        Assert.Single(results, x => !x);
        var final = await fixture.ReadReservationAsync(ids[0]);
        var second = await fixture.ReadReservationAsync(ids[1]);
        Assert.Equal(1, new[] { final, second }.Count(x => x.Status == ReservationStatuses.Confirmed));
    }

    [Fact]
    public async Task CancellationRestoresAvailabilityAndAuditAndRejectsStaleTokens()
    {
        var scenario = await CreateReservationScenario();
        var actorId = await GrantReservationOperations(scenario,
            (scenario.FacilityScope, [SecurityOperations.Read, SecurityOperations.Update]),
            (scenario.EventScope, [SecurityOperations.Read, SecurityOperations.Update]),
            (scenario.ReservationScope, [SecurityOperations.Create, SecurityOperations.Update, SecurityOperations.Read]));
        using var actor = fixture.ForActor(actorId);
        var requested = await Request(actor, scenario, scenario.Start, scenario.Start.AddHours(1));
        var confirmed = await Confirm(actor, requested.id, "confirmed");
        var beforeCancel = await fixture.ReadReservationAsync(requested.id);
        var fullAvailability = await actor.Reservations.AvailabilityAsync(
            scenario.FacilityId, scenario.Start, scenario.Start.AddHours(1));
        Assert.False(fullAvailability.IsAvailable);

        var cancelled = await actor.Reservations.CancelAsync(
            requested.id, new(beforeCancel.updated_at, "cancelled by organizer"));
        Assert.Equal(ReservationStatuses.Cancelled, cancelled.Status);
        Assert.Equal("cancelled by organizer", cancelled.DecisionReason);
        Assert.Equal(actorId, cancelled.updated_by);
        Assert.Equal(requested.created_by, cancelled.created_by);
        Assert.NotEqual(confirmed.updated_at, cancelled.updated_at);

        var afterCancel = await actor.Reservations.AvailabilityAsync(
            scenario.FacilityId, scenario.Start, scenario.Start.AddHours(1));
        Assert.True(afterCancel.IsAvailable);
        Assert.Empty(await actor.Reservations.OccupancyAsync(
            scenario.FacilityId, scenario.Start, scenario.Start.AddHours(1)));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => actor.Reservations.RejectAsync(
            requested.id, new(beforeCancel.updated_at, "stale rejection")));
        var persisted = await fixture.ReadReservationAsync(requested.id);
        Assert.Equal(ReservationStatuses.Cancelled, persisted.Status);
        Assert.Equal("cancelled by organizer", persisted.DecisionReason);
    }

    [Fact]
    public async Task CancellationRequiresFacilityUpdateRatherThanOnlyRead()
    {
        var scenario = await CreateReservationScenario();
        var actorId = await fixture.CreateActorAsync("Read-only facility canceller");
        await fixture.GrantAsync(actorId, scenario.FacilityScope, SecurityOperations.Read);
        await fixture.GrantAsync(actorId, scenario.EventScope, SecurityOperations.Update);
        await fixture.GrantAsync(actorId, scenario.ReservationScope, SecurityOperations.Update);
        await fixture.RebuildAccessAsync();
        var reservationId = await fixture.SeedReservationAsync(scenario.FacilityId, scenario.EventId,
            scenario.ReservationScope, scenario.Start, scenario.Start.AddHours(1), ReservationStatuses.Confirmed);
        var token = (await fixture.ReadReservationAsync(reservationId)).updated_at;

        using var actor = fixture.ForActor(actorId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => actor.Reservations.CancelAsync(
            reservationId, new(token)));
        Assert.Equal(ReservationStatuses.Confirmed, (await fixture.ReadReservationAsync(reservationId)).Status);
    }

    private async Task<ReservationScenario> CreateReservationScenario()
    {
        var facilityScope = await fixture.CreateScopeAsync("Reservation facility scope");
        var eventScope = await fixture.CreateScopeAsync("Reservation event scope");
        var reservationScope = await fixture.CreateScopeAsync("Explicit reservation scope");
        var typeScope = await fixture.CreateScopeAsync("Reservation type scope");
        var typeId = await fixture.SeedFacilityTypeAsync(typeScope);
        var facilityId = await fixture.SeedFacilityAsync(facilityScope, typeId);
        var eventId = await fixture.SeedEventAsync(eventScope);
        return new(facilityScope, eventScope, reservationScope, facilityId, eventId, fixture.Now.AddDays(2));
    }

    private async Task<Guid> GrantReservationOperations(ReservationScenario scenario,
        params (Guid Scope, string[] Operations)[] grants)
    {
        var actorId = await fixture.CreateActorAsync("Facilities reservation actor");
        foreach (var (scope, operations) in grants)
            await fixture.GrantAsync(actorId, scope, operations);
        await fixture.RebuildAccessAsync();
        return actorId;
    }

    private static Task<ReservationResponse> Request(FacilitiesPostgresFixture.ActorServices actor,
        ReservationScenario scenario, DateTimeOffset start, DateTimeOffset end)
        => actor.Reservations.RequestAsync(new(
            scenario.FacilityId, scenario.EventId, start, end, scenario.ReservationScope));

    private async Task<ReservationResponse> Confirm(FacilitiesPostgresFixture.ActorServices actor,
        Guid reservationId, string reason)
    {
        var current = await fixture.ReadReservationAsync(reservationId);
        return await actor.Reservations.ConfirmAsync(reservationId, new(current.updated_at, reason));
    }

    private sealed record ReservationScenario(
        Guid FacilityScope, Guid EventScope, Guid ReservationScope,
        Guid FacilityId, Guid EventId, DateTimeOffset Start);
}
