# ZArchitecture77.2 — oprava HTTP 404 při kliknutí na Import dat

## Příčina

`/digital-processes/form-preview/actions/{actionId:guid}` byl registrován jen při `IHostEnvironment.IsDevelopment()`. V kontejneru spuštěném s jiným nebo chybějícím `ASPNETCORE_ENVIRONMENT` se route neregistrovala, i když tlačítko v HTML existovalo.

## Oprava

V `DigitalProcessesModule.MapEndpoints` je nyní `PreviewFormEndpoints.Map(endpoints)` bez podmínky prostředí. HTMX URL a signatura endpointu se nemění. Route `/digital-processes/form-preview/values` je také dostupná.

## Pozor na zabezpečení

Preview endpointy zůstávají experimentální. Nejde o produkčně připravené API; před vystavením nedůvěryhodným uživatelům doplňte serverovou autorizaci jednotlivých operací, CSRF ochranu a omezení přístupu k preview instancím. Do té doby omezte přístup síťově nebo prostřednictvím autentizační brány.

## Test

1. Sestavte a spusťte Host.
2. Otevřete `/swagger/v1/swagger.json` a ověřte v `paths` POST `/digital-processes/form-preview/actions/{actionId}`.
3. V Designer/Test klikněte na Import dat. Očekávaná odpověď již nebude routing 404; při chybě konfigurace se objeví textové hlášení v kartě.
