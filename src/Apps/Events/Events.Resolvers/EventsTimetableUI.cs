using Microsoft.AspNetCore.Http;
namespace WebAppArchitecture.Events;

/// <summary>UI-only representation of the Events JSON timetable contract.</summary>
public static class EventsTimetableUI
{
 public static TimeZoneInfo PragueZone()
 {
  try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague"); }
  catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); }
 }
 public static IResult Render() => Results.Content(Html,"text/html; charset=utf-8");
 private const string Html = """
<!doctype html>
<html lang="cs"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Rozvrh událostí</title>
<style>
:root{font-family:system-ui,sans-serif;color:#222}body{margin:1.5rem}a{color:#165b91}
.controls{display:flex;gap:1rem;align-items:end;flex-wrap:wrap;margin:1rem 0}
.controls label{display:flex;flex-direction:column;gap:.25rem}
.controls input,.controls button{font:inherit;padding:.35rem}
.timetable{width:100%;min-width:780px;border-collapse:collapse;table-layout:fixed}
.timetable th,.timetable td{border:1px solid #aaa;vertical-align:top}
.timetable .day{width:12%;padding:.45rem;text-align:left;background:white}
.timetable .time{width:88%;padding:0;background:white}
.hours{position:relative;height:34px;display:flex}
.hour{box-sizing:border-box;border-right:1px solid #ddd;flex:1;font-size:.8rem;text-align:left;padding:.4rem .15rem}
.track{position:relative;min-height:125px;background:repeating-linear-gradient(to right,#fff 0,#fff calc(100% / 11 - 1px),#ddd calc(100% / 11 - 1px),#ddd calc(100% / 11))}
.event{position:absolute;box-sizing:border-box;border:1px solid #347447;border-radius:5px;background:#d7ecdc;min-height:95px;overflow:auto;margin-top:5px;padding:0;font-size:.85rem}
.event header{padding:.3rem;background:#b6d8be;font-weight:700;text-align:center}
.event .content{padding:.35rem}.event h3{font-size:.95rem;margin:.2rem 0}.event small{display:block;color:#555}
.outside{margin:.4rem;font-size:.8rem;color:#8c4e00}
.wrapper{overflow-x:auto}.muted{color:#666}.empty{padding:1rem}#error{color:#a00}
</style></head><body>
<h1>Rozvrh událostí</h1><p class="muted">Časová osa 07:00–18:00, časové pásmo Europe/Prague. Události jsou vybírány ze stromu a mohou přesahovat svého rodiče.</p>
<div class="controls"><label>Od <input id="from" type="date"></label><label>Do <input id="to" type="date"></label><button id="load" type="button">Zobrazit</button></div>
<div class="wrapper"><table class="timetable"><thead><tr><th class="day">Den</th><th class="time"><div id="hours" class="hours"></div></th></tr></thead><tbody id="days"></tbody></table></div>
<p id="error" role="status"></p>
<script>
'use strict';
const zone='Europe/Prague',hourStart=7,hourEnd=18;
const $=id=>document.getElementById(id);
const dateFormat=new Intl.DateTimeFormat('cs-CZ',{timeZone:zone,day:'2-digit',month:'2-digit',year:'numeric',weekday:'long'});
const timeFormat=new Intl.DateTimeFormat('cs-CZ',{timeZone:zone,hour:'2-digit',minute:'2-digit',hourCycle:'h23'});
const localParts=new Intl.DateTimeFormat('en-CA',{timeZone:zone,year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'});
function localInfo(value){const p=Object.fromEntries(localParts.formatToParts(new Date(value)).filter(x=>x.type!=='literal').map(x=>[x.type,x.value]));return {date:`${p.year}-${p.month}-${p.day}`,minutes:Number(p.hour)*60+Number(p.minute)}}
function plusDay(s,n){const d=new Date(s+'T12:00:00Z');d.setUTCDate(d.getUTCDate()+n);return d.toISOString().slice(0,10)}
function dateList(a,b){let dates=[];for(let d=a;d<=b&&dates.length<31;d=plusDay(d,1))dates.push(d);return dates}
function header(){for(let h=hourStart;h<hourEnd;h++){const node=document.createElement('div');node.className='hour';node.textContent=String(h).padStart(2,'0')+':00';$('hours').append(node)}}
function el(tag,cls,txt){const e=document.createElement(tag);if(cls)e.className=cls;if(txt!=null)e.textContent=txt;return e}
function render(data){$('days').replaceChildren();const types=new Map(data.types.map(t=>[t.id,t]));
for(const day of dateList(data.from,data.to)){
 const row=el('tr');row.id='day-'+day;const label=el('th','day');label.scope='row';label.append(el('strong',null,dateFormat.format(new Date(day+'T12:00:00Z'))));row.append(label);
 const td=el('td','time'),track=el('div','track');td.append(track);row.append(td);$('days').append(row);
 const items=[];for(const ev of data.events){const a=localInfo(ev.startsAt),b=localInfo(ev.endsAt);if(a.date>day||b.date<day)continue;
  const first=a.date===day?a.minutes:0,last=b.date===day?b.minutes:1440;
  const visibleStart=Math.max(hourStart*60,first),visibleEnd=Math.min(hourEnd*60,last);
  if(visibleStart<visibleEnd)items.push({ev,from:visibleStart,to:visibleEnd,a,b});}
 // Stable greedy lane assignment for overlapping lessons in the same day.
 items.sort((a,b)=>a.from-b.from||a.to-b.to);const laneEnds=[];
 for(const item of items){let lane=laneEnds.findIndex(v=>v<=item.from);if(lane<0){lane=laneEnds.length;laneEnds.push(item.to)}else laneEnds[lane]=item.to;item.lane=lane;}
 const lanes=Math.max(1,laneEnds.length);track.style.minHeight=Math.max(125,lanes*112+9)+'px';
 for(const item of items){const t=types.get(item.ev.eventTypeId),card=el('article','event');
  card.style.left=((item.from-hourStart*60)/(60*(hourEnd-hourStart))*100)+'%';
  card.style.width=Math.max(0.4,(item.to-item.from)/(60*(hourEnd-hourStart))*100)+'%';
  card.style.top=(item.lane*112)+'px';
  const head=el('header',null,timeFormat.format(new Date(item.ev.startsAt))+' – '+timeFormat.format(new Date(item.ev.endsAt))+'  '+(t?.abbreviation||''));
  card.append(head);const content=el('div','content');const title=el('h3');const link=el('a',null,item.ev.name);link.href=location.pathname.replace('/timetable/','/view/').replace(/\/view\/[^/]+$/,'/view/'+item.ev.id);title.append(link);content.append(title);
  if(t?.name)content.append(el('small',null,t.name));content.append(el('small',null,'ID: '+item.ev.id));
  if(item.a.date!==item.b.date)content.append(el('small',null,'Událost pokračuje přes více dní'));
  card.append(content);track.append(card);
 }
 if(!items.length)track.append(el('div','empty','Žádné události v zobrazeném časovém intervalu.'));
}}
async function load(){const from=$('from').value,to=$('to').value;if(!from||!to)return;if(to<from||dateList(from,to).length>30&&plusDay(from,30)<=to){$('error').textContent='Rozsah smí být nejvýše 31 dní.';return}
 const url=new URL(location.href);url.searchParams.set('from',from);url.searchParams.set('to',to);
 try{const r=await fetch(url,{headers:{Accept:'application/json'}});if(!r.ok)throw new Error('HTTP '+r.status);const data=await r.json();render(data);$('error').textContent='';history.replaceState(null,'',url)}catch(err){$('error').textContent='Rozvrh se nepodařilo načíst: '+err.message}}
header();const nowParts=localInfo(new Date().toISOString());const params=new URLSearchParams(location.search);$('from').value=params.get('from')||nowParts.date;$('to').value=params.get('to')||plusDay($('from').value,6);$('load').onclick=load;load();
</script></body></html>
""";
}
