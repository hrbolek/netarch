# ARCHITECTURE_UPDATE_126 – Events: denní hodinový rozvrh

- Přidáno specializované UI `EventsTimetableUI` se stupnicí 07:00–18:00, jednotlivými dny a kartami událostí s polohou/délkou určenou z DateTimeOffset.
- Vícedenní události jsou oříznuty **pouze vizuálně** na konkrétní den a zobrazené hodiny; uložený interval se nemění.
- Překrývající se události se rozloží do samostatných vertikálních drah.
- `GET /events/EventResponse/timetable/{id}?from=YYYY-MM-DD&to=YYYY-MM-DD`: `Accept: text/html` zobrazí rozvrh v UI Hostu, `Accept: application/json` vrátí typy a RBAC-filtrované události z podstromu.
- Hlavní akademická období a segmenty plánovače nejsou vykreslovány jako celodenní překryv hodinového rozvrhu; hodinový pohled nyní zahrnuje události kratší než sedm dní.
- Výchozí okno je sedm dní, maximum 31 dní; kalendářní dny a hodiny používají `Europe/Prague`.
- Zachován společný strom `EventModel` a existující `EventsModule`. Žádný zásah do Core.
- V uživatelské ukázce byly místnost, vyučující, předmět, studijní skupina, katedra a areál. Aktuální `EventModel` tyto reference **nemá**, a proto je nová karta nevymýšlí; zobrazuje název, typ, čas a identifikátor. Budoucí enrichment přes rozhraní jiných modulů je otevřený.
- Nelze ověřit `dotnet build` ani `dotnet test`: SDK není v dostupném běhovém prostředí.
