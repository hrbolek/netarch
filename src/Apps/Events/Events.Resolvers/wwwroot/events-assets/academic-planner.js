/* Progressive enhancement for server-rendered Razor AcademicPlannerPage; no React dependency. */
(() => {
  function init(root) {
    if(root.dataset.initialized) return; root.dataset.initialized='true';
    const id=root.dataset.yearId, canEdit=root.dataset.editable==='true';
    const api='/events/EventResponse/academic-plan/'+encodeURIComponent(id);
    const value=(o,...names)=>names.map(k=>o?.[k]).find(v=>v!==undefined&&v!==null);
    const day=s=>String(s).slice(0,10);
    const plus=(s,n)=>{const d=new Date(day(s)+'T12:00:00Z');d.setUTCDate(d.getUTCDate()+n);return d.toISOString().slice(0,10)};
    const colorText=hex=>{const h=String(hex||'').replace('#',''); if(!/^[0-9a-f]{6}$/i.test(h))return '#fff';
      const rgb=[0,2,4].map(i=>parseInt(h.slice(i,i+2),16));
      return (rgb[0]*299+rgb[1]*587+rgb[2]*114)/1000>=160?'#000':'#fff'};
    const overlap=(a,b,c,d)=>a<=d&&c<=b;
    const tools=Array.from(root.querySelectorAll('.ap-tool'));
    const toolMap=new Map(tools.map(b=>[b.dataset.type,{id:b.dataset.type,name:b.dataset.name,abbr:b.dataset.abbr,color:b.dataset.color}]));
    const cells=Array.from(root.querySelectorAll('.ap-half'));
    const save=root.querySelector('.ap-save'),reset=root.querySelector('.ap-reset'),message=root.querySelector('.ap-message'),
          diffBox=root.querySelector('.ap-diff'),counter=root.querySelector('.ap-counter');
    let year=null, original=[], segments=[], selected=null, saving=false;
    const clone=a=>a.map(e=>({...e}));
    const normalized=(raw)=>({id:value(raw,'id'),typeId:value(raw,'eventTypeId','EventTypeId'),
       name:value(raw,'name','Name')||'',start:day(value(raw,'startsAt','StartsAt')),
       end:plus(day(value(raw,'endsAt','EndsAt')),-1)});
    const stable=arr=>arr.slice().sort((a,b)=>a.start.localeCompare(b.start)||a.end.localeCompare(b.end));
    function normalize(arr) {
      const sorted=stable(arr), out=[];
      for(const item of sorted) {
        const previous=out[out.length-1];
        if(previous && previous.typeId===item.typeId && plus(previous.end,1)>=item.start) {
          // A real UUID has priority over a client-side segment's temporary ID.
          const id=String(previous.id||'').startsWith('tmp-') && !String(item.id||'').startsWith('tmp-') ? item.id:previous.id;
          out[out.length-1]={...previous,id,start:previous.start<item.start?previous.start:item.start,
                              end:previous.end>item.end?previous.end:item.end};
        } else out.push(item);
      }
      return out;
    }
    function change(a,b) {
      if(!selected||!canEdit||saving)return;
      const matches=segments.filter(e=>e.typeId===selected.id&&overlap(e.start,e.end,a,b));
      const intervalsToAdd=[];let remaining=[{start:a,end:b}];
      for(const same of matches) remaining=remaining.flatMap(r=>{
        if(!overlap(r.start,r.end,same.start,same.end))return [r];
        const next=[];if(r.start<same.start)next.push({start:r.start,end:plus(same.start,-1)});
        if(same.end<r.end)next.push({start:plus(same.end,1),end:r.end});
        return next;
      });
      intervalsToAdd.push(...remaining);
      const retained=[];
      for(const old of segments) {
        if(!overlap(old.start,old.end,a,b)){retained.push(old);continue}
        if(old.start<a)retained.push({...old,end:plus(a,-1)});
        if(old.end>b)retained.push({...old,id:old.start<a?'tmp-'+crypto.randomUUID():old.id,start:plus(b,1)});
      }
      segments=normalize(retained.concat(intervalsToAdd.map(r=>({...r,id:'tmp-'+crypto.randomUUID(),
        typeId:selected.id,name:selected.name}))));
      render();
    }
    function changes(){
      const old=new Map(original.filter(x=>x.id&&!String(x.id).startsWith('tmp-')).map(x=>[x.id,x]));
      const now=new Map(segments.map(x=>[x.id,x]));
      const added=segments.filter(x=>!old.has(x.id));
      const deleted=original.filter(x=>!now.has(x.id));
      const updated=segments.filter(x=>old.has(x.id)&&['start','end','typeId'].some(k=>old.get(x.id)[k]!==x[k]));
      return {added,updated,deleted};
    }
    function render(){
      for(const cell of cells) {
        const hit=segments.find(e=>overlap(cell.dataset.start,cell.dataset.end,e.start,e.end));
        const t=toolMap.get(hit?.typeId);
        const clr=t?.color||'#777777';
        cell.style.setProperty('--fill',clr);
        cell.querySelector('span').textContent=t?.abbr||'';
        cell.querySelector('span').style.color=colorText(clr);
        cell.title=(t?.name||'Bez události')+' — '+cell.dataset.start+' až '+cell.dataset.end;
      }
      const diff=changes(),count=diff.added.length+diff.updated.length+diff.deleted.length;
      if(counter)counter.textContent=count?`${diff.added.length} přidáno, ${diff.updated.length} změněno, ${diff.deleted.length} odstraněno`:'Bez neuložených změn';
      if(save)save.disabled=!count||saving;if(reset)reset.disabled=!count||saving;
      if(diffBox)diffBox.textContent=JSON.stringify(diff,null,2);
    }
    tools.forEach(b=>b.addEventListener('click',()=>{
      selected=toolMap.get(b.dataset.type);
      tools.forEach(t=>t.classList.toggle('ap-selected',t===b));
    }));
    cells.forEach(c=>c.addEventListener('click',()=>change(c.dataset.start,c.dataset.end)));
    reset?.addEventListener('click',()=>{segments=clone(original);render();message.textContent='Změny vráceny.'});
    save?.addEventListener('click',async()=>{
      if(saving||!canEdit)return;saving=true;render();message.textContent='Ukládám…';
      const request={expectedUpdatedAt:value(year,'updated_at','updatedAt','UpdatedAt'),
         segments:segments.map(e=>({id:String(e.id||'').startsWith('tmp-')?null:e.id,eventTypeId:e.typeId,
         name:e.name||toolMap.get(e.typeId)?.name||'Období',startsAt:e.start+'T00:00:00Z',
         endsAt:plus(e.end,1)+'T00:00:00Z'}))};
      try {
        const response=await fetch(api,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(request)});
        if(!response.ok)throw Error('HTTP '+response.status+' — změny nebyly uloženy; načtěte aktuální data.');
        message.textContent='Uloženo.';await load();
      } catch(err){message.textContent=String(err)}
      finally{saving=false;render()}
    });
    async function load(){
      const response=await fetch(api,{headers:{Accept:'application/json'}});
      if(!response.ok)throw Error('Nelze načíst harmonogram (HTTP '+response.status+')');
      const data=await response.json();year=data.year;
      original=normalize((data.events||[]).filter(e=>value(e,'isPlannerSegment','IsPlannerSegment')===true&&
        String(value(e,'parentId','ParentId'))===id).map(normalized));
      segments=clone(original);render();
    }
    load().catch(err=>{message.textContent=String(err)});
  }
  function start(){document.querySelectorAll('.academic-planner').forEach(init)}
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',start);else start();
})();
