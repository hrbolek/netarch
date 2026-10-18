# ZArchitecture 125 — Events

## Implementováno

- Nová aplikace `src/Apps/Events` jako samostatný .NET projekt; přidána do Host a solution. Žádná změna Core.
- Nezávislé stromové hierarchie EventType a Event; čas potomka nemusí ležet v čase rodiče.
- `DateTimeOffset` Start/End; plánovací UI pracuje s datumovou částí.
- `EventRegistrationPolicyModel` (volitelné 1:1), `EventParticipantModel` (User nebo Group), samostatné číselníky Source/Status/Role.
- Seed historických UUID pro invited, accepted, declined, attended, excused a organizer. Organizační role nepřiděluje RBAC.
- EF model module configuration, core security lookup pro čtení i privilegované CUD.
- API endpoints pro entity, stromy, účastníky, pozvánky, vlastní registraci, odpovědi, schválení a docházku.
- Speciální aplikací vlastněný HTML akademický harmonogram (bez React/CDN): ISO týdny půlené na Po–St/Čt–Ne, barevné segmenty, tabulkový editor, transakční PUT a kontrola verze rodiče. Editují se pouze přímé podřízené události označené `IsPlannerSegment`.

## URI

- `/events/EventResponse/view/{id}`, `/vector`, `/children/{id}`, `/create`, `/update/{id}`, `/delete/{id}`.
- `/events/EventTypeResponse/tree`, `/create`, `/update/{id}`, `/delete/{id}`.
- `/events/EventResponse/academic-plan/{id}` (GET JSON, PUT); v UI buildu stejné GET URI s `Accept: text/html` vrací HTML.
- `/events/EventResponse/registration-policy/{id}`, `/participants/{id}`, `/invite/{id}`, `/register/{id}`.
- `/events/EventParticipantResponse/accept/{id}`, `/decline/{id}`, `/approve/{id}`, `/record-attendance/{id}`.

## Otevřené body a omezení

- Současný Host převzal tuto aplikaci do existujícího grafu projektu 124; tato verze nedokončuje celkovou separaci API/UI ostatních modulů.
- API implementováno jako Minimal API uvnitř Events. Navázání běžných detailů na generické `LargeCard`/`ViewVector` není ještě hotové; specializovaný plán je zatím aplikací vlastněná HTML reprezentace.
- Akademický editor normalizuje a rozděluje půltýdenní segmenty, ale nepokrývá plnou sémantiku původního React prototypu (např. drag výběr).
- `EventRegistrationPolicy` kapacita se kontroluje aplikačně; vysoká souběžnost by vyžadovala databázově serializované rezervace.
- Žádné automaticky vygenerované EF migrace nebo integrační testy.
- .NET SDK nebylo v tomto prostředí dostupné; compile/test dosud neověřeny.
