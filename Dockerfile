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

COPY src/Base/Base.csproj src/Base/
COPY src/Group.Models/Group.Models.csproj src/Group.Models/
COPY src/Group.Resolvers/Group.Resolvers.csproj src/Group.Resolvers/
COPY src/Group.Services/Group.Services.csproj src/Group.Services/
COPY src/Group.UI/Group.UI.csproj src/Group.UI/
COPY src/Host/Host.csproj src/Host/
COPY src/Membership.Models/Membership.Models.csproj src/Membership.Models/
COPY src/Membership.Resolvers/Membership.Resolvers.csproj src/Membership.Resolvers/
COPY src/Membership.Services/Membership.Services.csproj src/Membership.Services/
COPY src/Membership.UI/Membership.UI.csproj src/Membership.UI/
COPY src/Role.Models/Role.Models.csproj src/Role.Models/
COPY src/Role.Resolvers/Role.Resolvers.csproj src/Role.Resolvers/
COPY src/Role.Services/Role.Services.csproj src/Role.Services/
COPY src/Role.UI/Role.UI.csproj src/Role.UI/
COPY src/RoleType.Models/RoleType.Models.csproj src/RoleType.Models/
COPY src/RoleType.Resolvers/RoleType.Resolvers.csproj src/RoleType.Resolvers/
COPY src/RoleType.Services/RoleType.Services.csproj src/RoleType.Services/
COPY src/RoleType.UI/RoleType.UI.csproj src/RoleType.UI/
COPY src/User.Models/User.Models.csproj src/User.Models/
COPY src/User.Resolvers/User.Resolvers.csproj src/User.Resolvers/
COPY src/User.Services/User.Services.csproj src/User.Services/
COPY src/User.UI/User.UI.csproj src/User.UI/


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

RUN dotnet publish src/Host/Host.csproj \
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