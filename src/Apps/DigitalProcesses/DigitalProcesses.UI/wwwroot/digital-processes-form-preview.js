// Refreshes computed display.url links from the server-side recomputation returned by each autosave.
function dpApplyUrls(payload) {
  for (const item of payload?.urls || []) {
    document.querySelectorAll('[data-form-backend-kind="display-url"][data-form-node-id="' + item.id + '"]').forEach(box => {
      const link = box.querySelector('[data-url-link]'), missing = box.querySelector('[data-url-missing]');
      if (!link || !missing) return;
      link.hidden = !item.url; missing.hidden = !!item.url;
      if (item.url) { link.href = item.url; link.target = item.target; link.textContent = item.text || item.url; }
      else { link.removeAttribute('href'); missing.title = item.error || ''; }
    });
  }
}
document.addEventListener('htmx:afterRequest', event => {
  if (event.detail.elt?.id !== 'dp-preview-form' || !event.detail.successful) return;
  try { dpApplyUrls(JSON.parse(event.detail.xhr.responseText)); } catch { }
});
// Sent by the import action (HX-Trigger) after the backend values were stored.
document.addEventListener('dpUrls', event => dpApplyUrls(event.detail));
window.digitalProcessesRenumber = function(root) {
  document.querySelectorAll('[data-repeat-body]').forEach(body => {
    body.querySelectorAll(':scope > [data-repeat-instance]').forEach((instance, index) => {
      instance.dataset.occurrence = String(index);
    });
  });
  (root || document).querySelectorAll('input[name^="node-"],textarea[name^="node-"],select[name^="node-"]').forEach(el => {
    const base = el.dataset.nodeBase || el.name.split('__')[0];
    el.dataset.nodeBase = base;
    const chain = [];
    let current = el;
    while (current) {
      const instance = current.closest('[data-repeat-instance]');
      if (!instance) break;
      const section = instance.closest('section[data-form-node-id]');
      if (section?.dataset.repeatSection === 'true') chain.unshift(instance.dataset.occurrence || '0');
      current = section?.parentElement || null;
    }
    el.name = base + (chain.length ? '__' + chain.join('_') : '');
    el.id = el.name;
    const label = el.closest('[data-form-node-id]')?.querySelector(':scope > label');
    if (label) label.htmlFor = el.id;
    const field = el.closest('[id^="form-field-"]');
    if (field) field.id = 'form-field-' + el.name;
    const search = el.closest('[data-graphql-search]');
    if (search) {
      const input = search.querySelector('[data-search-input]');
      const options = search.querySelector('[data-search-options]');
      input.id = el.name + '-search';
      options.id = input.id + '-options';
      input.setAttribute('aria-controls', options.id);
      search.querySelector('label').htmlFor = input.id;
    }
  });
};
window.digitalProcessesAddOccurrence = function(button) {
  const section = button.closest('section[data-form-node-id]');
  const body = section?.querySelector('[data-repeat-body]');
  const first = body?.querySelector(':scope > [data-repeat-instance]');
  if (!first) return;
  const max = Number(section.dataset.maxOccurs || '1');
  const instances = body.querySelectorAll(':scope > [data-repeat-instance]');
  if (instances.length >= max) return;
  const clone = first.cloneNode(true);
  clone.querySelectorAll('[data-repeat-remove]').forEach(el => el.remove());
  clone.dataset.occurrence = String(instances.length);
  clone.querySelectorAll('input,textarea,select').forEach(el => {
    if (el.type === 'checkbox' || el.type === 'radio') el.checked = false;
    else if (el.tagName === 'SELECT') el.selectedIndex = 0;
    else el.value = '';
  });
  clone.querySelectorAll('[data-search-options]').forEach(el => { el.replaceChildren(); el.hidden = true; });
  clone.querySelectorAll('[data-search-status]').forEach(el => el.textContent = '');
  clone.querySelectorAll('[data-cell-editor]').forEach(el => el.hidden = true);
  clone.querySelectorAll('[data-cell-display]').forEach(el => el.hidden = false);
  clone.querySelectorAll('[id]').forEach(el => {
    const old = el.id;
    el.id = old + '-row-' + instances.length;
    clone.querySelectorAll('[for],[aria-controls]').forEach(label => {
      if (label.htmlFor === old) label.htmlFor = el.id;
      if (label.getAttribute('aria-controls') === old) label.setAttribute('aria-controls', el.id);
    });
  });
  const remove = document.createElement('button');
  remove.type = 'button'; remove.className = 'btn btn-sm btn-outline-danger mb-2';
  remove.dataset.repeatRemove = '';
  remove.textContent = 'Remove occurrence';
  remove.onclick = () => { clone.remove(); window.digitalProcessesRenumber(document); button.disabled = false; button.closest('form')?.dispatchEvent(new Event('change', {bubbles:true})); };
  if (clone.tagName === 'TR') clone.querySelector('[data-row-actions]').append(remove);
  else clone.prepend(remove);
  first.parentNode.append(clone);
  button.disabled = instances.length + 1 >= max;
  window.digitalProcessesRenumber(document);
  window.digitalProcessesSections?.refresh();
  button.closest('form')?.dispatchEvent(new Event('change', {bubbles:true}));
};
document.addEventListener('DOMContentLoaded', () => window.digitalProcessesRenumber(document));
document.addEventListener('htmx:afterSwap', () => window.digitalProcessesRenumber(document));
window.digitalProcessesRenumber(document);
