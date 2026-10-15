namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormDefinition;
/// <summary>Human-readable authorization guidance for the HTML form endpoint.</summary>
public static class FormCreateAuthorizationMessage
{
    public const string Guidance = "To create a form definition, an active user role must grant the Create operation on the RBAC group 'Create Form Definitions' (11000000-0000-0000-0000-000000000001). Ask your administrator to verify the user's active role, its RoleType→Operation assignments, the group's inheritance and EffectiveAccess projection. An administrator display name alone does not grant the permission.";

    public const string Html = """
        <!doctype html><html lang="en"><head><meta charset="utf-8" /><meta name="viewport" content="width=device-width,initial-scale=1" />
        <title>Permission required — Form definition</title>
        <style>body{font:16px system-ui,sans-serif;max-width:46rem;margin:4rem auto;padding:0 1rem;color:#20252a}main{border:1px solid #ddd;border-radius:12px;padding:2rem}h1{font-size:1.5rem}code{overflow-wrap:anywhere}a{color:#0757a6}</style>
        </head><body><main role="alert"><h1>Permission required</h1>
        <p>You cannot create a form definition with your current permissions. No changes have been saved.</p>
        <p>Contact your administrator and request the <strong>Create</strong> operation on the
        <strong>Create Form Definitions</strong> RBAC group.</p>
        <p>Scope ID: <code>11000000-0000-0000-0000-000000000001</code></p>
        <p>The administrator should verify your active role, the role type's operations, group inheritance, and EffectiveAccess.</p>
        <p><a href="/digital-processes/FormDefinitionResponse/list">Back to forms</a> ·
        <a href="/digital-processes/FormDefinitionResponse/create-permission">Check permission (JSON)</a></p>
        </main></body></html>
        """;
}
