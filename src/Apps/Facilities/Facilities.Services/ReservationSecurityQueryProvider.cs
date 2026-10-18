using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Events;

namespace WebAppArchitecture.Facilities;

/// <summary>
/// Applies the approved three-entity read policy to every generic secured read,
/// including Query, GetById, Where and vector queries inherited from Core.
/// No parallel paging/projection implementation is needed.
/// </summary>
internal sealed class ReservationSecurityQueryProvider(IAuthorizationLayer security) : ISecurityQueryProvider
{
    public IQueryable<TModel> Query<TModel>(string operation) where TModel : class, ISecuredEntityDBModel
    {
        if (typeof(TModel) != typeof(ReservationModel)) return security.Query<TModel>(operation);
        // Even when a public Where/Vector caller selects Update or another row
        // operation, returning reservation details still requires related Read.
        // Private mutation lookups bypass this read adapter, not these safeguards.
        var facilities = security.Query<FacilityModel>(SecurityOperations.Read).Select(x => x.id);
        var events = security.Query<EventModel>(SecurityOperations.Read).Select(x => x.id);
        var readableReservations = security.Query<ReservationModel>(SecurityOperations.Read).Select(x => x.id);
        var reservations = security.Query<ReservationModel>(operation)
            .Where(x => readableReservations.Contains(x.id)
                && facilities.Contains(x.FacilityId) && events.Contains(x.EventId));
        // The closed-type identity above makes this cast safe; the SQL expression remains composed.
        return (IQueryable<TModel>)(object)reservations;
    }
}
