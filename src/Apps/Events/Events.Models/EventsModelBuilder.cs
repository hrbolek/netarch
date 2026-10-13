using Microsoft.EntityFrameworkCore;
namespace WebAppArchitecture.Events;
public static class EventsModelBuilder
{
 public static void ConfigureEvents(this ModelBuilder b)
 {
  b.Entity<EventTypeModel>(e=>{e.ToTable("event_types");e.HasKey(x=>x.id);e.HasIndex(x=>x.Code).IsUnique();e.HasOne<EventTypeModel>().WithMany().HasForeignKey(x=>x.ParentId).OnDelete(DeleteBehavior.Restrict);});
  b.Entity<EventModel>(e=>{e.ToTable("events");e.HasKey(x=>x.id);e.HasIndex(x=>x.ParentId);e.HasIndex(x=>x.IsPlannerSegment);e.HasIndex(x=>new{x.StartsAt,x.EndsAt});e.HasOne<EventModel>().WithMany().HasForeignKey(x=>x.ParentId).OnDelete(DeleteBehavior.Restrict);e.HasOne<EventTypeModel>().WithMany().HasForeignKey(x=>x.EventTypeId).OnDelete(DeleteBehavior.Restrict);});
  b.Entity<EventRegistrationPolicyModel>(e=>{e.ToTable("event_registration_policies");e.HasKey(x=>x.id);e.HasIndex(x=>x.EventId).IsUnique();e.HasOne<EventModel>().WithMany().HasForeignKey(x=>x.EventId).OnDelete(DeleteBehavior.Cascade);});
  b.Entity<EventParticipationSourceModel>(e=>{e.ToTable("event_participation_sources");e.HasKey(x=>x.id);e.HasIndex(x=>x.Code).IsUnique();});
  b.Entity<EventParticipantStatusModel>(e=>{e.ToTable("event_participant_statuses");e.HasKey(x=>x.id);e.HasIndex(x=>x.Code).IsUnique();});
  b.Entity<EventParticipantRoleModel>(e=>{e.ToTable("event_participant_roles");e.HasKey(x=>x.id);e.HasIndex(x=>x.Code).IsUnique();});
  b.Entity<EventParticipantModel>(e=>{e.ToTable("event_participants");e.HasKey(x=>x.id);e.HasIndex(x=>new{x.EventId,x.UserId}).IsUnique();e.HasIndex(x=>new{x.EventId,x.GroupId}).IsUnique();e.ToTable(t=>t.HasCheckConstraint("ck_event_participant_target","""("UserId" IS NOT NULL AND "GroupId" IS NULL) OR ("UserId" IS NULL AND "GroupId" IS NOT NULL)"""));e.HasOne<EventModel>().WithMany().HasForeignKey(x=>x.EventId).OnDelete(DeleteBehavior.Cascade);e.HasOne<EventParticipationSourceModel>().WithMany().HasForeignKey(x=>x.SourceId).OnDelete(DeleteBehavior.Restrict);e.HasOne<EventParticipantStatusModel>().WithMany().HasForeignKey(x=>x.StatusId).OnDelete(DeleteBehavior.Restrict);e.HasOne<EventParticipantRoleModel>().WithMany().HasForeignKey(x=>x.RoleId).OnDelete(DeleteBehavior.Restrict);});
  b.Entity<EventParticipantStatusModel>().HasData(
   new EventParticipantStatusModel{id=EventKnownIds.Invited,Code="invited",Name="Pozván",Description="Uživatel byl pozván na událost",IsInitial=true},
   new EventParticipantStatusModel{id=EventKnownIds.Accepted,Code="accepted",Name="Přijal",Description="Uživatel potvrdil účast"},
   new EventParticipantStatusModel{id=EventKnownIds.Declined,Code="declined",Name="Odmítl",Description="Uživatel odmítl účast",IsTerminal=true},
   new EventParticipantStatusModel{id=EventKnownIds.Attended,Code="attended",Name="Účastnil se",Description="Uživatel se účastnil události",IsTerminal=true},
   new EventParticipantStatusModel{id=EventKnownIds.Excused,Code="excused",Name="Omluven",Description="Uživatel se omluvil z účasti",IsTerminal=true},
   new EventParticipantStatusModel{id=EventKnownIds.PendingApproval,Code="pending_approval",Name="Čeká na schválení",IsInitial=true});
  b.Entity<EventParticipantRoleModel>().HasData(new EventParticipantRoleModel{id=EventKnownIds.Organizer,Code="organizer",Name="Organizátor"},new EventParticipantRoleModel{id=EventKnownIds.Participant,Code="participant",Name="Účastník"});
  b.Entity<EventParticipationSourceModel>().HasData(new EventParticipationSourceModel{id=EventKnownIds.Invitation,Code="invitation",Name="Pozvání"},new EventParticipationSourceModel{id=EventKnownIds.SelfRegistration,Code="self_registration",Name="Samostatná registrace"},new EventParticipationSourceModel{id=EventKnownIds.Assignment,Code="assignment",Name="Přiřazení"});
 }
}
