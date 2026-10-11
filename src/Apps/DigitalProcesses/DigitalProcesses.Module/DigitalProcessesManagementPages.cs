using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>
/// Contextual designer pages. Writes are routed to the existing RBAC-enforcing
/// Minimal APIs, never directly to DbContext from the browser.
/// </summary>
public static class DigitalProcessesManagementPages
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/digital-processes/process-versions/{id:guid}/permissions", ApplicantMatrix)
            .WithTags("DigitalProcesses.Designer")
            .WithDescription("Editable Applicant visibility / explicit deny matrix by workflow state and FormNode. Draft process versions only.");
        endpoints.MapGet("/digital-processes/form-versions/{id:guid}/templates", TemplatePage)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Contextual RTF template and immutable revision management for a FormVersion.");
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    // Standalone HTML endpoints cannot host a Blazor LargeCard directly. Use the
    // same two-column contextual layout with version links and an RBAC sidebar.
    private static string ContextShell(string title, string body, string kind, Guid id, Guid rbacId, string related) => Shell(title, $$"""
        <div class="card shadow-sm mb-4"><div class="card-header"><strong>{{E(kind)}}</strong> · {{id:D}}</div>
        <div class="card-body"><div class="row g-3">
          <aside class="col-12 col-md-3">
            <div class="card mb-3"><div class="card-body"><strong>{{E(kind)}}</strong>
              <p class="small text-muted mt-2 mb-1">ID: <code>{{id:D}}</code></p>
              <p class="small text-muted mb-0">RBAC: <code>{{rbacId:D}}</code></p></div></div>
            <nav class="list-group mb-3" aria-label="Contextual navigation">{{related}}</nav>
          </aside>
          <section class="col-12 col-md-9">{{body}}</section>
        </div></div></div>
        """);

    private static string Shell(string title, string body) => $$"""
        <!doctype html><html lang="cs"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
        <title>{{E(title)}}</title><link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css"></head>
        <body><main class="container py-4"><h1 class="h3">{{E(title)}}</h1>{{body}}</main></body></html>
        """;

    private static async Task<IResult> ApplicantMatrix(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var process = await security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Read, ct);
        if (process is null) return Results.NotFound();
        var mayEdit = !process.is_published &&
            await security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Update, ct) is not null;
        var states = security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Where(x => x.workflow_version_id == process.workflow_version_id)
            .OrderBy(x => x.position).ToArray();
        var nodes = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == process.form_version_id)
            .OrderBy(x => x.path).ThenBy(x => x.position).ToArray();
        var html = new StringBuilder();
        html.Append($"<p class='text-muted'>ProcessVersion <code>{id:D}</code>. Oprávnění se konfigurují před publikováním. " +
                    "<strong>Deny má přednost před zděděným přístupem</strong>; skrytá větev zůstává skrytá pro žadatele i při přechodu workflow.</p>");
        html.Append("<div class='alert alert-info'>Tato matice spravuje <strong>Applicant</strong> – explicitní čtení/deny. " +
                    "Jiné role se přiřazují pomocí pravidel WorkflowStateAccessRule a RBAC. Matice jim zatím nevytváří Deny.</div>");
        if (process.is_published) html.Append("<div class='alert alert-warning'>Publikovaná verze: pouze pro čtení. Pro změnu vytvoř nový draft.</div>");
        if (states.Length == 0) html.Append("<div class='alert alert-warning'>Tato verze workflow nemá žádné čitelné stavy. Nejprve vytvořte stavy ve workflow designeru (nebo ověřte oprávnění Read).</div>");
        if (nodes.Length == 0) html.Append("<div class='alert alert-warning'>Tato verze formuláře nemá žádné čitelné uzly. Nejprve vytvořte uzly ve form designeru (nebo ověřte oprávnění Read).</div>");
        if (!process.is_published && !mayEdit) html.Append("<div class='alert alert-warning'>Jde o draft, ale aktuální uživatel nemá operaci Update nad RBAC objektem této verze procesu. Editace je proto zakázána.</div>");
        html.Append("<div class='table-responsive'><table class='table table-sm table-bordered align-middle'><thead><tr><th>FormNode</th>");
        foreach (var state in states) html.Append($"<th>{E(state.name)}</th>");
        html.Append("</tr></thead><tbody>");
        foreach (var node in nodes)
        {
            var depth = Math.Clamp(node.path.Count(x => x == '/'), 0, 8);
            html.Append($"<tr><th style='padding-left:{depth * 16 + 8}px'>{E(node.label)} <small class='text-muted'>({E(node.key)})</small></th>");
            foreach (var state in states)
            {
                html.Append($"<td><select class='form-select form-select-sm policy' data-state='{state.id:D}' data-node='{node.id:D}' {(mayEdit ? "" : "disabled")}>" +
                            "<option value=''>Dědit / neurčeno</option><option value='true'>Read</option><option value='false'>Deny</option></select></td>");
            }
            html.Append("</tr>");
        }
        html.Append("</tbody></table></div><p id='status' role='status' class='small text-muted'></p>");
        // Existing policy PUT only sets true/false. The 'inherit' UI state is read-only until DELETE is implemented.
        html.Append($$$"""
            <script>
            const base='/digital-processes/ProcessDefinitionVersionResponse/applicant-access/{{{id:D}}}';
            const cells=[...document.querySelectorAll('select.policy')];
            fetch(base,{headers:{'Accept':'application/json'}}).then(r=>{if(!r.ok)throw Error('HTTP '+r.status);return r.json()}).then(rows=>{
              for(const item of rows){ const c=cells.find(x=>x.dataset.state===item.workflow_state_id&&x.dataset.node===item.form_node_id);
                if(c)c.value=String(item.can_read).toLowerCase(); }
            }).catch(e=>document.getElementById('status').textContent='Načtení pravidel: '+e.message);
            for(const cell of cells) cell.addEventListener('change',async()=>{
              if(cell.value===''){document.getElementById('status').textContent='Pro odstranění explicitního pravidla zatím použij API; neurčeno nelze uložit.';return;}
              cell.disabled=true;
              try {const r=await fetch(base,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify({stateId:cell.dataset.state,formNodeId:cell.dataset.node,canRead:cell.value==='true'})});
                if(!r.ok)throw Error('HTTP '+r.status+' '+await r.text()); document.getElementById('status').textContent='Pravidlo uloženo.';
              } catch(e){document.getElementById('status').textContent='Uložení selhalo: '+e.message;} finally{cell.disabled=false;}
            });
            </script>
            """);
        return Results.Content(ContextShell("Přístup k formuláři podle workflow – Applicant", html.ToString(),
            "Process version", id, process.rbacobject_id,
            $"<a class='list-group-item list-group-item-action' href='/digital-processes/ProcessDefinitionVersionResponse/view/{id:D}'>Detail verze procesu</a>" +
            $"<a class='list-group-item list-group-item-action' href='/digital-processes/FormVersionResponse/designer/{process.form_version_id:D}'>Form designer</a>" +
            $"<a class='list-group-item list-group-item-action' href='/digital-processes/WorkflowVersionResponse/designer/{process.workflow_version_id:D}'>Workflow designer</a>"), "text/html; charset=utf-8");
    }

    private static async Task<IResult> TemplatePage(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var version = await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Read, ct);
        if (version is null) return Results.NotFound();
        var mayEdit = !version.is_published &&
            await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Update, ct) is not null;
        var html = $$$"""
            <p class="text-muted">Šablony náleží ke konkrétní FormVersion <code>{{{id:D}}}</code>. Každá úprava se ukládá jako nová neměnná revize.</p>
            <div id="templates" class="list-group mb-4">Načítání...</div>
            {{{{{(mayEdit ? """
            <section class="card mb-4"><div class="card-body"><h2 class="h5">Nahrát novou RTF šablonu</h2>
            <label class="form-label">Název <input id="template-name" class="form-control" value="Rozhodnutí"></label>
            <label class="form-label">RTF soubor <input id="template-file" type="file" accept=".rtf,application/rtf" class="form-control"></label>
            <button id="upload" class="btn btn-primary">Nahrát šablonu</button>
            <button id="generate" class="btn btn-outline-secondary">Vygenerovat ze schématu</button></div></section>
            """ : "<div class='alert alert-info'>Publikovaná nebo nezapisovatelná verze: pouze čtení.</div>")}}}
            <section class="card"><div class="card-body"><h2 class="h5">AI asistent tvorby šablony</h2>
            <p class="text-muted">Budoucí agent převezme DOCX/RTF/PDF předlohu a schéma FormNodes, navrhne placeholdery a vazby.
            Před uložením nové revize proběhne kontrola návrhu uživatelem.</p>
            <input type="file" class="form-control mb-2" disabled aria-label="Vzorový dokument pro AI">
            <button class="btn btn-outline-secondary" disabled>Analyzovat vzor pomocí AI (připravuje se)</button></div></section>
            <div id="status" role="status" class="mt-3 small"></div>
            <script>
            const root='/digital-processes'; const version='{{{id:D}}}'; const status=document.getElementById('status');
            async function reload(){const r=await fetch(`${root}/FormVersionResponse/rtf-templates/${version}`,{headers:{Accept:'application/json'}});
              if(!r.ok)throw Error('HTTP '+r.status);const items=await r.json(); const list=document.getElementById('templates');list.replaceChildren();
              for(const t of items){const id=t.id??t.Id;const name=t.name??t.Name;const row=document.createElement('div');row.className='list-group-item';
                const link=document.createElement('a');link.href=`${root}/RtfTemplate/${id}`;link.textContent=name;row.append(link);
                const revisions=document.createElement('a');revisions.className='ms-3';revisions.href=`${root}/document-templates/${id}/revisions`;revisions.textContent='Revize (JSON)';row.append(revisions);list.append(row);}
              if(!items.length)list.textContent='Zatím žádná šablona.';
            }
            reload().catch(e=>status.textContent=e.message);
            const upload=document.getElementById('upload');if(upload)upload.onclick=async()=>{try{const f=document.getElementById('template-file').files[0];if(!f)throw Error('Vyber RTF soubor.');
                const name=encodeURIComponent(document.getElementById('template-name').value);const r=await fetch(`${root}/FormVersionResponse/rtf-templates/${version}?name=${name}`,{method:'POST',headers:{'Content-Type':'application/rtf'},body:f});
                if(!r.ok)throw Error(await r.text());status.textContent='Šablona uložena.';await reload();}catch(e){status.textContent=e.message;}};
            const generate=document.getElementById('generate');if(generate)generate.onclick=async()=>{try{const r=await fetch(`${root}/FormVersionResponse/rtf-template/generate/${version}`,{method:'POST'});
                if(!r.ok)throw Error(await r.text());status.textContent='Šablona vygenerována.';await reload();}catch(e){status.textContent=e.message;}};
            </script>
            """;
        return Results.Content(ContextShell("RTF šablony formuláře", html, "Form version", id, version.rbacobject_id,
            $"<a class='list-group-item list-group-item-action' href='/digital-processes/FormVersionResponse/view/{id:D}'>Detail verze formuláře</a>" +
            $"<a class='list-group-item list-group-item-action' href='/digital-processes/FormVersionResponse/designer/{id:D}'>Form designer</a>" +
            $"<a class='list-group-item list-group-item-action' href='/digital-processes/FormVersionResponse/test/{id:D}'>Test formuláře</a>"), "text/html; charset=utf-8");
    }
}
