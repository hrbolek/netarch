// Node action menus are <details>; close them after choosing an item, on outside click and on Escape.
document.addEventListener('click', event => {
  document.querySelectorAll('details.dp-node-actions[open]').forEach(menu => {
    if (!menu.contains(event.target) || event.target.closest('.dropdown-item')) menu.removeAttribute('open');
  });
});
document.addEventListener('keydown', event => {
  if (event.key === 'Escape') document.querySelectorAll('details.dp-node-actions[open]').forEach(m => m.removeAttribute('open'));
});
