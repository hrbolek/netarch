# ZArchitecture77.8

- Import tlačítko se nevykresluje jako vlastní karta; pouze tlačítko a informace o výsledku.
- HTMX out-of-band nahrazuje existující obaly formulářových prvků `form-field-{id}` (včetně label a input), nikoli vykreslování nových vstupů v kartě.
- Zachováno mapování hodnot relativně k rodičovské sekci akce.
- Nastaveny požadované logovací kategorie v src/Host/appsettings.json.
- Sestavení .NET je nutné ověřit ve vývojovém kontejneru.
