# ZArchitecture71 — sjednocení reprezentací v Core

- User, Group, Role a Membership: `GET /create` používá jediný společný `CreatePageModel` s `ApplyViewModel`.
- `POST /create-form` vrací stejné `{Entity}Response` jako `/view/{id}`; HTML reprezentuje stávající `{Entity}ViewPage`. Nevrací již automatický redirect.
- Typované `/create-contained`, `/create-typed`, `/update`, `/list`, `/view`, `/edit`, `/actions`, `/link` již využívají stávající sjednocené modely a byly ponechány.
- 400/401/403 a delete bez vrácené entity zůstávají odlišnými HTTP výsledky.
- Speciální bezpečnostní `create-role-form` a navigační přesměrování vyžadují samostatnou doménovou revizi; nevytváříme pro ně fiktivní entity.
- Pozor: HTML create-form nyní vykreslí detail nové entity místo redirectu; jde o záměrnou změnu HTTP chování.
- Testy .NET SDK v tomto prostředí nejsou dostupné; proveďte `dotnet build src/Host/Host.csproj`.
