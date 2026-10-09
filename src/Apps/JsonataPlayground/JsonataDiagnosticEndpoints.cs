using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using WebAppArchitecture.DigitalProcesses.Services;

namespace WebAppArchitecture.JsonataPlayground;

public static class JsonataDiagnosticEndpoints
{
    public static void Map(WebApplication app)
    {
        // Nothing is mapped outside Development. Protect the development instance too.
        if (!app.Environment.IsDevelopment()) return;
        app.MapGet("/jsonata", () => Results.Content(PlaygroundHtml, "text/html; charset=utf-8"))
           .WithName("JsonataPlayground").WithTags("Development")
           .WithDescription("Development-only playground for the configured DigitalProcesses GraphQL backend.");

        app.MapPost("/jsonata/execute", async (HttpContext ctx, JsonataDiagnosticInput input,
            FormDataBackendOptions options, IHttpClientFactory factory, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(options.Endpoint))
                return Results.Json(new {error="Missing DigitalProcesses:DataBackend:Endpoint"},statusCode:400);
            if (string.IsNullOrWhiteSpace(input.Query))
                return Results.Json(new {error="Query is required"},statusCode:400);

            using var outgoing = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
            outgoing.Content = JsonContent.Create(new { query=input.Query, variables=input.Variables });
            FormBackendHeaderForwarder.Apply(ctx.Request, outgoing, options);
            var requestHeaders = outgoing.Headers.ToDictionary(x=>x.Key, x=>x.Value.ToArray(),StringComparer.OrdinalIgnoreCase);
            var contentHeaders = outgoing.Content.Headers.ToDictionary(x=>x.Key,x=>x.Value.ToArray(),StringComparer.OrdinalIgnoreCase);
            var requestView = new {method="POST",url=options.Endpoint,headers=requestHeaders,contentHeaders,
                body=new {query=input.Query,variables=input.Variables}};
            var watch=Stopwatch.StartNew();
            try
            {
                using var response = await factory.CreateClient().SendAsync(outgoing,ct);
                var raw = await response.Content.ReadAsStringAsync(ct);
                JsonNode? parsed=null;
                try { parsed=JsonNode.Parse(raw); } catch (JsonException) { }
                var responseHeaders = response.Headers.ToDictionary(x=>x.Key,x=>x.Value.ToArray(),StringComparer.OrdinalIgnoreCase);
                var responseContentHeaders = response.Content.Headers.ToDictionary(x=>x.Key,x=>x.Value.ToArray(),StringComparer.OrdinalIgnoreCase);
                return Results.Json(new {upstreamStatus=(int)response.StatusCode,elapsedMs=watch.ElapsedMilliseconds,
                    request=requestView,response=new {headers=responseHeaders,contentHeaders=responseContentHeaders,
                    body=parsed,rawBody=raw}});
            }
            catch(Exception ex) when(ex is HttpRequestException or TaskCanceledException)
            {
                return Results.Json(new {error=ex.Message,elapsedMs=watch.ElapsedMilliseconds,request=requestView},statusCode:502);
            }
        }).WithName("JsonataExecute").WithTags("Development")
          .WithDescription("Development-only GraphQL proxy showing exact forwarded headers and full HTTP payload. Sensitive information is returned to the caller.");
    }

    internal sealed record JsonataDiagnosticInput(string Query, JsonNode? Variables);

    private const string PlaygroundHtml = """
<!DOCTYPE html>
<html lang="cs"><head><meta charset="UTF-8"/><meta name="viewport" content="width=device-width, initial-scale=1"/><title>JSONata / GraphQL – diagnostika</title>
<style>body{font:15px system-ui,sans-serif;max-width:1200px;margin:30px auto;padding:0 18px;color:#182535}textarea{box-sizing:border-box;width:100%;min-height:170px;font:13px/1.4 ui-monospace,monospace;padding:12px;border:1px solid #aab6c5;border-radius:6px}#variables{min-height:85px}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#f2f5f8;padding:16px;border-radius:6px;max-height:500px;overflow:auto}button{padding:9px 18px;background:#164e85;color:white;border:0;border-radius:6px;cursor:pointer}button:disabled{opacity:.5}small{color:#536276}.columns{display:grid;grid-template-columns:1fr 1fr;gap:18px}@media(max-width:800px){.columns{grid-template-columns:1fr}}.summary{padding:10px;background:#f2f5f8;border-radius:6px}details{margin:14px 0}summary{cursor:pointer;font-weight:600}.warning{padding:10px;border-left:4px solid #d08418;background:#fff7e8}</style></head><body>
<h1>JSONata Federation Playground</h1><p>Testuje stejný backend a <code>ForwardHeaders</code> jako DigitalProcesses import. Zobrazuje přesně odeslané hlavičky a odpověď.</p>
<p class="warning"><strong>Pouze Development.</strong> Diagnostika může zobrazovat cookies, access tokeny a osobní údaje. Nepoužívej ji na veřejně dostupném hostu.</p>
<label for="query"><strong>GraphQL query</strong></label><textarea id="query">query { whoami { hostname } }</textarea>
<label for="variables"><strong>Variables (JSON)</strong></label><textarea id="variables">{}</textarea><p><button id="execute">Execute</button> <small>POST /jsonata/execute (server-to-server, stejný endpoint jako import)</small></p>
<div class="summary" id="summary">Připraveno.</div>
<div class="columns"><section><h2>Odchozí HTTP požadavek</h2><pre id="request">–</pre></section><section><h2>HTTP odpověď a GraphQL data/errors</h2><pre id="response">–</pre></section></div>
<details><summary>Kompletní diagnostický JSON</summary><pre id="full">–</pre></details>
<script>
const $=id=>document.getElementById(id);
$('execute').addEventListener('click',async()=>{
 $('execute').disabled=true;$('summary').textContent='Načítám...';
 try{
   const variables=JSON.parse($('variables').value||'{}');
   const resp=await fetch('/jsonata/execute',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({query:$('query').value,variables})});
   const out=await resp.json();
   $('summary').textContent=`Proxy HTTP ${resp.status}; upstream HTTP ${out.upstreamStatus??'—'}; ${out.elapsedMs??'—'} ms; ${out.error??''}`;
   $('request').textContent=JSON.stringify(out.request??{},null,2);
   $('response').textContent=JSON.stringify(out.response??{},null,2);
   $('full').textContent=JSON.stringify(out,null,2);
 }catch(e){$('summary').textContent=String(e);}
 finally{$('execute').disabled=false;}
});
</script></body></html>
""";
}
