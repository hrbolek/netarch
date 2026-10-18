# Update 72.1 – oprava duplicitních deklarací po refaktoringu

## Příčina

Při rozbalování ZIPu 72 přes existující pracovní adresář zůstalo 17 původních souborů z verze 71. SDK kompiluje standardně všechny `**/*.cs`, takže zdrojové deklarace z původních souborů kolidovaly s novými soubory rozdělenými podle typů (CS0101, CS0111, CS8863).

## Oprava

- Ve všech dotčených `.csproj` jsou explicitní `Compile Remove` pro původní soubory, aby i při překryvném rozbalení nevstupovaly do kompilace.
- Skript `tools/cleanup-obsolete-sources-72.sh` fyzicky odstraní pouze uvedených 17 souborů z projektu. Je vhodné jej spustit při aktualizaci existujícího workspace.
- Jména veřejných typů, namespace a chování API se nemění.

## Nasazení

```bash
unzip -o ZArchitecture72_1.zip -d /workspace
cd /workspace
bash tools/cleanup-obsolete-sources-72.sh
dotnet clean src/Host/Host.csproj
dotnet build src/Host/Host.csproj
```

Případně rozbalte archiv do **zcela prázdného** adresáře. Kopírování přes starší verzi bez mazání zastaralých souborů je rizikové.

Kompilace nebyla v prostředí autora spuštěna (chybí SDK .NET); XML projektu a integrita ZIP archivu se ověřují staticky.

### Vyřazené soubory

- `src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessCommands.cs`
- `src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessResponses.cs`
- `src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/FormBackendContracts.cs`
- `src/Apps/DigitalProcesses/DigitalProcesses.Services/Services/Integration/FormDataBackend.cs`
- `src/Core/Base/Data/BaseStore.cs`
- `src/Core/Base/Data/EntityDBModel.cs`
- `src/Core/Base/Security/RbacSecurity.cs`
- `src/Core/Base/UI/Components/EntityActionModels.cs`
- `src/Core/DBLayer/DBModels.cs`
- `src/Core/DBLayer/DigitalProcessesDBModels.cs`
- `src/Core/DBLayer/EfStores.cs`
- `src/Core/DBLayer/StoreContracts.cs`
- `src/Core/Group/Group.Models/GroupModel.cs`
- `src/Core/Membership/Membership.Models/MembershipModel.cs`
- `src/Core/Role/Role.Models/RoleModel.cs`
- `src/Core/RoleType/RoleType.Models/RoleTypeModel.cs`
- `src/Core/User/User.Models/UserModel.cs`
