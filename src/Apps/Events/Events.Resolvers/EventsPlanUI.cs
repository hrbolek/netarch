using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Base.Security;
namespace WebAppArchitecture.Events;
public static class EventsPlanUI
{
 public static async Task<IResult> Render(Guid id,HttpContext http,ICurrentActorProvider actors,EventService svc,CoreDbContext db,CancellationToken ct)
 {
  var actor=actors.TryGetActorUserId(http);
  if(actor is null)return Results.Unauthorized();
  var year=await svc.FindAsync(id,ct);
  if(year is null)return Results.NotFound();
  var editable=await svc.CanAsync(year,"update",ct);
  var html="""
<!doctype html><html lang="cs"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Harmonogram akademického roku</title>
<style>body{font:14px system-ui;margin:2rem}h1{margin-bottom:.2rem}.muted{color:#666}.weekgrid{display:flex;gap:3px;overflow:auto;padding:8px 0}.week{min-width:45px;text-align:center;font-size:11px}.halves{display:flex;min-height:42px}.half{flex:1;min-width:20px;border:1px solid #bbb;cursor:pointer;font-size:10px}table{border-collapse:collapse;width:100%;max-width:1100px}td,th{border:1px solid #ddd;padding:7px}input,select,button{padding:5px}button{cursor:pointer}.toolbar{display:flex;gap:8px;flex-wrap:wrap;margin:1rem 0}#message{margin:1rem 0}.disabled{pointer-events:none;opacity:.7}</style></head><body>
<h1 id="year">Akademický harmonogram</h1><p class="muted">Zobrazení používá kalendářní data; databáze ukládá DateTimeOffset. Potomci mohou přesahovat interval rodiče.</p>
<div class="toolbar"><label>Typ období <select id="type"></select></label><button id="add">Přidat interval</button><button id="save">Uložit změny</button></div>
<div id="weeks" class="weekgrid"></div><p class="muted">Každý týden je rozdělen na Po–St a Čt–Ne. Kliknutí označí půltýden vybraným typem. Ruční úpravy jsou možné v tabulce.</p>
<table><thead><tr><th>Událost</th><th>Typ</th><th>Od</th><th>Do (včetně)</th><th></th></tr></thead><tbody id="rows"></tbody></table><p id="message" role="status"></p>
<script>
const api=location.pathname;let data,segments=[],canEdit=__EDITABLE__;
const $=id=>document.getElementById(id), iso=d=>d.toISOString().slice(0,10);
const parse=s=>new Date(s.slice(0,10)+'T00:00:00Z');
const shift=(d,n)=>{let x=new Date(d);x.setUTCDate(x.getUTCDate()+n);return x};
const overlap=(a,b,c,d)=>a<=d&&c<=b;
const uiEnd=s=>iso(shift(parse(s.endsAt),-1));
const start=s=>iso(parse(s.startsAt));
const form=(s,i)=>{const tr=document.createElement('tr');const input=(value,type='text')=>{let x=document.createElement('input');x.type=type;x.value=value;return x};
 let name=input(s.name),type=document.createElement('select');for(let t of data.types){let o=document.createElement('option');o.value=t.id;o.textContent=t.name;type.append(o)}type.value=s.eventTypeId;
 let from=input(start(s),'date'),to=input(uiEnd(s),'date'),del=document.createElement('button');del.textContent='Smazat';del.onclick=()=>{segments.splice(i,1);render()};
 for(let node of [name,type,from,to,del]){let td=document.createElement('td');td.append(node);tr.append(td)}
 for(let [el,key] of [[name,'name'],[type,'eventTypeId']])el.onchange=()=>{s[key]=el.value;renderWeeks()};
 from.onchange=()=>{s.startsAt=from.value+'T00:00:00Z';renderWeeks()};to.onchange=()=>{s.endsAt=iso(shift(parse(to.value),1))+'T00:00:00Z';renderWeeks()};
 if(!canEdit)for(let el of [name,type,from,to,del])el.disabled=true;
 return tr};
const render=()=>{const body=$('rows');body.replaceChildren();segments.forEach((s,i)=>body.append(form(s,i)));renderWeeks()};
const initTypes=()=>{const select=$('type');select.replaceChildren();data.types.forEach(t=>{const o=document.createElement('option');o.value=t.id;o.textContent=t.name;select.append(o)})};
const weeks=(startDate,endDate)=>{let d=parse(startDate);let day=d.getUTCDay()||7;d=shift(d,1-day);let result=[];for(let n=0;n<65&&d<parse(endDate);n++,d=shift(d,7))result.push(new Date(d));return result};
const renderWeeks=()=>{const root=$('weeks');root.replaceChildren();if(!data)return;
 for(let monday of weeks(data.year.startsAt,data.year.endsAt)){let node=document.createElement('div');node.className='week';let label=document.createElement('div');label.textContent=iso(monday).slice(5);node.append(label);let halves=document.createElement('div');halves.className='halves';
 for(let [offset,len] of [[0,3],[3,4]]){let a=iso(shift(monday,offset)),b=iso(shift(monday,offset+len-1));let hit=segments.find(s=>overlap(start(s),uiEnd(s),a,b));let button=document.createElement('button');button.className='half';let type=hit&&data.types.find(t=>t.id===hit.eventTypeId);button.textContent=type?.abbreviation||'';button.style.backgroundColor=type?.color||'#f3f3f3';button.title=a+' až '+b+(type?' – '+type.name:'');button.disabled=!canEdit;
 button.onclick=()=>{let selected=data.types.find(t=>t.id===$('type').value);if(!selected)return;
 const same=hit&&hit.eventTypeId===selected.id;
 const next=[];
 for(let old of segments){let lo=start(old),hi=uiEnd(old);if(!overlap(lo,hi,a,b)){next.push(old);continue}
 // Subtract only the painted half-week. Preserve the original UUID for the first surviving fragment.
 if(lo<a)next.push({...old,endsAt:a+'T00:00:00Z'});
 if(hi>b)next.push({...old,id:lo<a?null:old.id,startsAt:iso(shift(parse(b),1))+'T00:00:00Z'});
 }
 if(!same)next.push({id:null,eventTypeId:selected.id,name:selected.name,startsAt:a+'T00:00:00Z',endsAt:iso(shift(parse(b),1))+'T00:00:00Z'});
 segments=next;render()};halves.append(button)}node.append(halves);root.append(node)}};
$('add').onclick=()=>{let t=data.types.find(t=>t.id===$('type').value);if(!t)return;segments.push({id:null,eventTypeId:t.id,name:t.name,startsAt:data.year.startsAt,endsAt:data.year.endsAt});render()};
$('save').onclick=async()=>{const payload={expectedUpdatedAt:data.year.updated_at||data.year.updatedAt,segments:segments.map(({id,eventTypeId,name,startsAt,endsAt})=>({id,eventTypeId,name,startsAt,endsAt}))};try{let r=await fetch(api,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)});$('message').textContent=r.ok?'Uloženo. Obnovuji data…':'Uložení odmítnuto ('+r.status+'). Obnovte stránku, mohou existovat souběžné změny.';if(r.ok)location.reload()}catch(err){$('message').textContent=String(err)}};
Promise.all([fetch(api).then(r=>{if(!r.ok)throw Error('HTTP '+r.status);return r.json()})]).then(([result])=>{data=result;$('year').textContent=result.year.name;initTypes();segments=result.events.filter(x=>x.isPlannerSegment&&x.parentId===result.year.id).map(x=>({...x}));render();if(!canEdit){$('save').disabled=true;$('add').disabled=true}}).catch(err=>$('message').textContent='Nelze načíst harmonogram: '+err);
</script></body></html>
""";
  return Results.Content(html.Replace("__EDITABLE__",editable?"true":"false"),"text/html; charset=utf-8");
 }
}
