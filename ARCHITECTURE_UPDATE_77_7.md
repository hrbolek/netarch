# ZArchitecture 77.7 – scoped GraphQL import

- Import mapuje GraphQL `data` relativně k rodičovské sekci uzlu akce.
- Najde pouze existující vstupní pole v podstromu této sekce; nesouvisející části formuláře nemění.
- Karta importu nepřidává nové readonly inputy z GraphQL odpovědi. Odpovídající pole aktualizuje HTMX `hx-swap-oob`.
- Do logu zapisuje `incomingKeys` a `matchedKeys`, nikoliv hodnoty polí.
- Otestovat: akce a sourozenecká sekce `whoami` s polem `hostname`, `query { whoami { hostname } }`.
- Limity: pouze text, number, date, textarea; preview stav dočasný, produkční autorizaci zatím nutno dokončit.
