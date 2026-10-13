# ZArchitecture 135 — Events UI

- Společná navigace `EventsMenu.razor` napříč stránkami Events.
- `EventResponse/list`, `EventResponse/new`, `EventResponse/detail/{id}`, `EventResponse/edit/{id}`.
- Detail obsahuje přepnutí na akademický harmonogram a hodinový rozvrh, podřízené události a akce editace/mazání podle RBAC.
- `EventTypeResponse/list`, `EventTypeResponse/new`, `EventTypeResponse/edit/{id}`.
- Původní API a importní endpointy zachovány, `view/{id}` při HTML Accept přesměrovává na detail.
- Seznam událostí nadále filtruje záznamy podle oprávnění read.
- Žádné změny Core ani databázového schématu.
- Kontrola ZIP integrity provedena, sestavení a integrační testy nelze ověřit bez .NET SDK.
