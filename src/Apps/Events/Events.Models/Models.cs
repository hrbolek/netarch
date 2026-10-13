using WebAppArchitecture.Base.Data;
namespace WebAppArchitecture.Events;
public abstract class EventEntity : ISecuredEntityDBModel, WebAppArchitecture.Base.Data.IEntityDTOModel
{
 public Guid id {get;set;} = Guid.NewGuid();
 public Guid rbacobject_id {get;set;}
 public DateTimeOffset created_at {get;set;} = DateTimeOffset.UtcNow;
 public DateTimeOffset updated_at {get;set;} = DateTimeOffset.UtcNow;
 public Guid created_by {get;set;}
 public Guid updated_by {get;set;}
}
public abstract class EventTreeEntity : EventEntity
{
 public Guid? ParentId {get;set;}
 /// <summary>Materialized path of all ancestors: /{rootId}/.../{id}/ (maintained by TreeSecuredService).</summary>
 public string path {get;set;} = "";
}
public sealed class EventTypeModel : EventTreeEntity, ITreeEntityDBModel<EventTypeModel>
{
 Guid? ITreeEntityDBModel<EventTypeModel>.parent_id => ParentId;
 public EventTypeModel WithPath(string newPath){path=newPath;return this;}
 public string Code {get;set;} = "";
 public string Name {get;set;} = "";
 public string? Description {get;set;}
 public string? Abbreviation {get;set;}
 public string? Color {get;set;}
 public string RegistrationMode {get;set;} = "inherit";
}
public sealed class EventModel : EventTreeEntity, ITreeEntityDBModel<EventModel>
{
 Guid? ITreeEntityDBModel<EventModel>.parent_id => ParentId;
 public EventModel WithPath(string newPath){path=newPath;return this;}
 public Guid EventTypeId {get;set;}
 public string Name {get;set;} = "";
 public string? Description {get;set;}
 public bool IsPlannerSegment {get;set;}
 public DateTimeOffset StartsAt {get;set;}
 public DateTimeOffset EndsAt {get;set;}
}
public sealed class EventRegistrationPolicyModel : EventEntity
{
 public Guid EventId {get;set;}
 public int? Capacity {get;set;}
 public DateTimeOffset? OpensAt {get;set;}
 public DateTimeOffset? ClosesAt {get;set;}
}
public sealed class EventParticipationSourceModel
{
 public Guid id {get;set;}
 public string Code {get;set;} = "";
 public string Name {get;set;} = "";
}
public sealed class EventParticipantStatusModel
{
 public Guid id {get;set;}
 public string Code {get;set;} = "";
 public string Name {get;set;} = "";
 public string? Description {get;set;}
 public bool IsInitial {get;set;}
 public bool IsTerminal {get;set;}
}
public sealed class EventParticipantRoleModel
{
 public Guid id {get;set;}
 public string Code {get;set;} = "";
 public string Name {get;set;} = "";
}
public sealed class EventParticipantModel : EventEntity
{
 public Guid EventId {get;set;}
 public Guid? UserId {get;set;}
 public Guid? GroupId {get;set;}
 public Guid SourceId {get;set;}
 public Guid StatusId {get;set;}
 public Guid? RoleId {get;set;}
 public DateTimeOffset? RespondedAt {get;set;}
 public DateTimeOffset? StatusChangedAt {get;set;}
}
public static class EventKnownIds
{
 public static readonly Guid Invited = Guid.Parse("01b96c1d-8389-4267-a859-d8116e1c32f3");
 public static readonly Guid Accepted = Guid.Parse("7d2ef223-b60e-4e6d-b7d5-5fdc1f8e2ec2");
 public static readonly Guid Declined = Guid.Parse("d6a5e9e4-3e47-4c95-a4aa-b194dd2bc3a7");
 public static readonly Guid Attended = Guid.Parse("d7c38ef9-c7d0-4ff9-a72e-fd1e0b70f387");
 public static readonly Guid Excused = Guid.Parse("b0b2df1d-4e67-47c3-9c68-4d25a0e0a01a");
 public static readonly Guid Organizer = Guid.Parse("3265a488-bbfa-4c59-946c-7a7b059ee4f0");
 public static readonly Guid Invitation = Guid.Parse("a9a5aff3-c8cd-4dd9-8248-142d5028bdf2");
 public static readonly Guid SelfRegistration = Guid.Parse("4c17ed6f-b149-4552-8da0-50f3d8770324");
 public static readonly Guid Assignment = Guid.Parse("6a98f445-cc04-4664-bf03-469aa77bc61c");
 public static readonly Guid Participant = Guid.Parse("f4ef4d25-ed76-447d-86ed-4d35346995f4");
 public static readonly Guid PendingApproval = Guid.Parse("e27be0d6-3774-4378-bafb-8d596741da71");
}
