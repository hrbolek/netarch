namespace WebAppArchitecture.Events;
public sealed record AcademicPlannerModel(
    EventModel Year,
    IReadOnlyList<EventModel> Segments,
    IReadOnlyList<EventTypeModel> Types,
    bool CanEdit);
