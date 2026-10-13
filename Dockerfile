# ============================================================
# Build
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src


# ------------------------------------------------------------
# Copy solution and project files first.
# This keeps NuGet restore cached when source files change.
# ------------------------------------------------------------

COPY SignFakeArchitecture.sln ./
COPY Directory.Build.props ./

COPY src/Apps/DigitalProcesses/DigitalProcesses.Models/DigitalProcesses.Models.csproj src/Apps/DigitalProcesses/DigitalProcesses.Models/
COPY src/Apps/DigitalProcesses/DigitalProcesses.Resolvers/DigitalProcesses.Resolvers.csproj src/Apps/DigitalProcesses/DigitalProcesses.Resolvers/
COPY src/Apps/DigitalProcesses/DigitalProcesses.Services/DigitalProcesses.Services.csproj src/Apps/DigitalProcesses/DigitalProcesses.Services/
COPY src/Apps/DigitalProcesses/DigitalProcesses.UI/DigitalProcesses.UI.csproj src/Apps/DigitalProcesses/DigitalProcesses.UI/
COPY src/Core/Base/Base.csproj src/Core/Base/
COPY src/Core/DBLayer/DBLayer.csproj src/Core/DBLayer/
COPY src/Core/Group/Group.Models/Group.Models.csproj src/Core/Group/Group.Models/
COPY src/Core/Group/Group.Resolvers/Group.Resolvers.csproj src/Core/Group/Group.Resolvers/
COPY src/Core/Group/Group.Services/Group.Services.csproj src/Core/Group/Group.Services/
COPY src/Core/Group/Group.UI/Group.UI.csproj src/Core/Group/Group.UI/
COPY src/Core/Membership/Membership.Models/Membership.Models.csproj src/Core/Membership/Membership.Models/
COPY src/Core/Membership/Membership.Resolvers/Membership.Resolvers.csproj src/Core/Membership/Membership.Resolvers/
COPY src/Core/Membership/Membership.Services/Membership.Services.csproj src/Core/Membership/Membership.Services/
COPY src/Core/Membership/Membership.UI/Membership.UI.csproj src/Core/Membership/Membership.UI/
COPY src/Core/Role/Role.Models/Role.Models.csproj src/Core/Role/Role.Models/
COPY src/Core/Role/Role.Resolvers/Role.Resolvers.csproj src/Core/Role/Role.Resolvers/
COPY src/Core/Role/Role.Services/Role.Services.csproj src/Core/Role/Role.Services/
COPY src/Core/Role/Role.UI/Role.UI.csproj src/Core/Role/Role.UI/
COPY src/Core/RoleType/RoleType.Models/RoleType.Models.csproj src/Core/RoleType/RoleType.Models/
COPY src/Core/RoleType/RoleType.Resolvers/RoleType.Resolvers.csproj src/Core/RoleType/RoleType.Resolvers/
COPY src/Core/RoleType/RoleType.Services/RoleType.Services.csproj src/Core/RoleType/RoleType.Services/
COPY src/Core/RoleType/RoleType.UI/RoleType.UI.csproj src/Core/RoleType/RoleType.UI/
COPY src/Core/Security/Security.Models/Security.Models.csproj src/Core/Security/Security.Models/
COPY src/Core/Security/Security.Resolvers/Security.Resolvers.csproj src/Core/Security/Security.Resolvers/
COPY src/Core/Security/Security.Services/Security.Services.csproj src/Core/Security/Security.Services/
COPY src/Core/Security/Security.UI/Security.UI.csproj src/Core/Security/Security.UI/
COPY src/Core/SecurityLayer/SecurityLayer.csproj src/Core/SecurityLayer/
COPY src/Core/User/User.Models/User.Models.csproj src/Core/User/User.Models/
COPY src/Core/User/User.Resolvers/User.Resolvers.csproj src/Core/User/User.Resolvers/
COPY src/Core/User/User.Services/User.Services.csproj src/Core/User/User.Services/
COPY src/Core/User/User.UI/User.UI.csproj src/Core/User/User.UI/
COPY src/Apps/Host/Host.csproj src/Apps/Host/


# ------------------------------------------------------------
# Restore
# ------------------------------------------------------------

RUN dotnet restore SignFakeArchitecture.sln


# ------------------------------------------------------------
# Copy sources
# ------------------------------------------------------------

COPY src/ ./src/


# ------------------------------------------------------------
# Build / publish host
# ------------------------------------------------------------

RUN dotnet publish src/Apps/Host/Host.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore


# ============================================================
# Runtime
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app


# ------------------------------------------------------------
# ASP.NET Core
# ------------------------------------------------------------

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production


# ------------------------------------------------------------
# Copy published application
# ------------------------------------------------------------

COPY --from=build /app/publish .


EXPOSE 8080


ENTRYPOINT ["dotnet", "Host.dll"]