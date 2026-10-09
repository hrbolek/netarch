using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public sealed record SystemDataImportResult(
    int Users,
    int Groups,
    int GroupTypes,
    int Roles,
    int RoleTypes,
    int Memberships);

/// <summary>
/// Imports only the system-data sections used by this application from the
/// _uois systemdata JSON. Unknown sections are intentionally ignored.
///
/// Supported sections:
/// users, groups, grouptypes, roles, roletypes and (when present)
/// memberships/groupmemberships.
/// </summary>
