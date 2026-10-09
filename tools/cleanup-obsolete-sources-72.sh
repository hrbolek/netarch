#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
# Files removed by the 71 -> 72 per-type refactoring. This script
# removes only obsolete filenames, never the replacement files.
if [ -f "src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessCommands.cs" ]; then rm -- "src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessCommands.cs"; echo "Removed src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessCommands.cs"; fi
if [ -f "src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessResponses.cs" ]; then rm -- "src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessResponses.cs"; echo "Removed src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/DigitalProcessResponses.cs"; fi
if [ -f "src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/FormBackendContracts.cs" ]; then rm -- "src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/FormBackendContracts.cs"; echo "Removed src/Apps/DigitalProcesses/DigitalProcesses.Models/Models/FormBackendContracts.cs"; fi
if [ -f "src/Apps/DigitalProcesses/DigitalProcesses.Services/Services/Integration/FormDataBackend.cs" ]; then rm -- "src/Apps/DigitalProcesses/DigitalProcesses.Services/Services/Integration/FormDataBackend.cs"; echo "Removed src/Apps/DigitalProcesses/DigitalProcesses.Services/Services/Integration/FormDataBackend.cs"; fi
if [ -f "src/Core/Base/Data/BaseStore.cs" ]; then rm -- "src/Core/Base/Data/BaseStore.cs"; echo "Removed src/Core/Base/Data/BaseStore.cs"; fi
if [ -f "src/Core/Base/Data/EntityDBModel.cs" ]; then rm -- "src/Core/Base/Data/EntityDBModel.cs"; echo "Removed src/Core/Base/Data/EntityDBModel.cs"; fi
if [ -f "src/Core/Base/Security/RbacSecurity.cs" ]; then rm -- "src/Core/Base/Security/RbacSecurity.cs"; echo "Removed src/Core/Base/Security/RbacSecurity.cs"; fi
if [ -f "src/Core/Base/UI/Components/EntityActionModels.cs" ]; then rm -- "src/Core/Base/UI/Components/EntityActionModels.cs"; echo "Removed src/Core/Base/UI/Components/EntityActionModels.cs"; fi
if [ -f "src/Core/DBLayer/DBModels.cs" ]; then rm -- "src/Core/DBLayer/DBModels.cs"; echo "Removed src/Core/DBLayer/DBModels.cs"; fi
if [ -f "src/Core/DBLayer/DigitalProcessesDBModels.cs" ]; then rm -- "src/Core/DBLayer/DigitalProcessesDBModels.cs"; echo "Removed src/Core/DBLayer/DigitalProcessesDBModels.cs"; fi
if [ -f "src/Core/DBLayer/EfStores.cs" ]; then rm -- "src/Core/DBLayer/EfStores.cs"; echo "Removed src/Core/DBLayer/EfStores.cs"; fi
if [ -f "src/Core/DBLayer/StoreContracts.cs" ]; then rm -- "src/Core/DBLayer/StoreContracts.cs"; echo "Removed src/Core/DBLayer/StoreContracts.cs"; fi
if [ -f "src/Core/Group/Group.Models/Models/GroupModel.cs" ]; then rm -- "src/Core/Group/Group.Models/Models/GroupModel.cs"; echo "Removed src/Core/Group/Group.Models/Models/GroupModel.cs"; fi
if [ -f "src/Core/Membership/Membership.Models/Models/MembershipModel.cs" ]; then rm -- "src/Core/Membership/Membership.Models/Models/MembershipModel.cs"; echo "Removed src/Core/Membership/Membership.Models/Models/MembershipModel.cs"; fi
if [ -f "src/Core/Role/Role.Models/Models/RoleModel.cs" ]; then rm -- "src/Core/Role/Role.Models/Models/RoleModel.cs"; echo "Removed src/Core/Role/Role.Models/Models/RoleModel.cs"; fi
if [ -f "src/Core/RoleType/RoleType.Models/Models/RoleTypeModel.cs" ]; then rm -- "src/Core/RoleType/RoleType.Models/Models/RoleTypeModel.cs"; echo "Removed src/Core/RoleType/RoleType.Models/Models/RoleTypeModel.cs"; fi
if [ -f "src/Core/User/User.Models/Models/UserModel.cs" ]; then rm -- "src/Core/User/User.Models/Models/UserModel.cs"; echo "Removed src/Core/User/User.Models/Models/UserModel.cs"; fi
echo "Obsolete source cleanup completed."
