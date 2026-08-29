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

COPY src/Base/Base.csproj src/Base/
COPY src/Host/Host.csproj src/Host/

# Add generated/module projects here if they are part
# of the solution.
#
# Example:
#
# COPY src/Document.Models/Document.Models.csproj src/Document.Models/
# COPY src/Document.Services/Document.Services.csproj src/Document.Services/
# COPY src/Document.Resolvers/Document.Resolvers.csproj src/Document.Resolvers/
# COPY src/Document.UI/Document.UI.csproj src/Document.UI/


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