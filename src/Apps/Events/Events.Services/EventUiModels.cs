namespace WebAppArchitecture.Events;

public sealed record EventDetailModel(EventModel Event,List<EventModel> Children,bool CanEdit,bool CanDelete);

public sealed record EventTypeEditModel(EventTypeModel Type,EventFormsModel Forms);
