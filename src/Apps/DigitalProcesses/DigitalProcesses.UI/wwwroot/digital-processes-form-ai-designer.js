(function () {
  function initAiDesigner() {
    const root = document.querySelector('[data-ai-form-version]');
    if (!root || root.dataset.aiReady === '1') return;
    root.dataset.aiReady = '1';
    const id = root.dataset.aiFormVersion;
    const instruction = document.getElementById('dp-ai-instruction');
    const status = document.getElementById('dp-ai-status');
    const box = document.getElementById('dp-ai-result');
    const json = document.getElementById('dp-ai-json');
    const diff = document.getElementById('dp-ai-diff');
    const propose = document.getElementById('dp-ai-propose');
    const approve = document.getElementById('dp-ai-approve');
    const existingPaths = new Set();
    // The designer contains the current node hierarchy and shows key/path per node.
    document.querySelectorAll('input.form-node-path').forEach(el => {
      if (el.value.startsWith('$.form.')) existingPaths.add(el.value.slice(7));
    });
    function collect(children, prefix, added) {
      Object.entries(children || {}).forEach(([key, node]) => {
        const path = prefix ? prefix + '.' + key : key;
        if (!existingPaths.has(path)) added.push(path);
        collect(node.children, path, added);
      });
    }
    function explain(schema) {
      const added = [];
      collect(schema.children, '', added);
      diff.textContent = added.length ? 'Nové uzly (' + added.length + '): ' + added.join(', ') :
        'Nebyly rozpoznány nové uzly. Zkontrolujte změny vlastností ve výsledném JSON.';
    }
    async function readResponse(response) {
      const payload = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(payload.error || payload.title || ('HTTP ' + response.status));
      return payload;
    }
    propose.addEventListener('click', async () => {
      const prompt = instruction.value.trim();
      if (!prompt) { status.textContent = 'Nejprve zadejte pokyn.'; return; }
      propose.disabled = true; box.hidden = true; status.textContent = 'Vyhodnocování pokynu…';
      try {
        const response = await fetch('/digital-processes/ai/form-design', {
          method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({formVersionId: id, instruction: prompt})
        });
        const payload = await readResponse(response);
        if (!payload.result || typeof payload.result.children !== 'object') throw new Error('Agent nevrátil strom formuláře.');
        json.value = JSON.stringify(payload.result, null, 2);
        explain(payload.result); box.hidden = false; status.textContent = 'Návrh je připraven. Před uložením jej zkontrolujte.';
      } catch (error) { status.textContent = 'Návrh selhal: ' + error.message; }
      finally { propose.disabled = false; }
    });
    approve.addEventListener('click', async () => {
      let schema;
      try { schema = JSON.parse(json.value); explain(schema); }
      catch (error) { status.textContent = 'Neplatný JSON: ' + error.message; return; }
      if (!confirm('Opravdu uložit schválený návrh do této FormVersion?')) return;
      approve.disabled = true; status.textContent = 'Ukládání…';
      try {
        const response = await fetch('/digital-processes/ai/form-design/approve', {
          method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({formVersionId: id, schema})
        });
        await readResponse(response);
        location.reload();
      } catch (error) { status.textContent = 'Uložení selhalo: ' + error.message; approve.disabled = false; }
    });
  }
  document.addEventListener('DOMContentLoaded', initAiDesigner);
  document.addEventListener('htmx:afterSwap', initAiDesigner);
  initAiDesigner();
})();
