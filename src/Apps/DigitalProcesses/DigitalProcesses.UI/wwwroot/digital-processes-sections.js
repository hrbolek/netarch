(() => {
    if (window.digitalProcessesSections) {
        window.digitalProcessesSections.refresh();
        return;
    }
    const displayText = cell => {
        const editor = cell.querySelector("[data-cell-editor]");
        const selected = editor?.querySelector("[data-search-value]");
        if (selected) {
            if (!selected.value) return "\u2014";
            try {
                return JSON.parse(selected.value)?.label || "\u2014";
            } catch {
                return "Invalid selected value";
            }
        }
        const input = editor?.querySelector("input:not([type=hidden]), textarea, select");
        if (!input) return "\u2014";
        if (input.type === "checkbox") return input.checked ? "Yes" : "No";
        if (input.tagName === "SELECT") return input.value ? input.selectedOptions[0]?.textContent || input.value : "\u2014";
        return input.value || "\u2014";
    };
    const refreshCell = cell => {
        const button = cell.querySelector("[data-cell-display]");
        const text = displayText(cell);
        if (button && button.textContent !== text) button.textContent = text;
    };
    const finishCell = cell => {
        refreshCell(cell);
        cell.querySelector("[data-cell-editor]").hidden = true;
        cell.querySelector("[data-cell-display]").hidden = false;
        const table = cell.closest("[data-section-table]");
        if (table && !table.querySelector("[data-cell-editor]:not([hidden]) [data-graphql-search]"))
            table.style.removeProperty("overflow");
    };
    const activateTab = tab => {
        const root = tab.closest("[data-form-tabs]");
        root.dataset.activeTab = tab.dataset.sectionTab;
        root.querySelectorAll("[data-section-tab]").forEach(button => {
            const active = button === tab;
            button.classList.toggle("active", active);
            button.setAttribute("aria-selected", String(active));
            button.tabIndex = active ? 0 : -1;
        });
        root.querySelectorAll("[data-section-panel]").forEach(panel => {
            panel.hidden = panel.dataset.sectionPanel !== tab.dataset.sectionTab;
        });
    };
    const updateNodeTypes = form => {
        const parent = form.querySelector("[data-node-parent]");
        const types = form.querySelector('select[name="node_type_id"]');
        if (!parent || !types) return;
        [...types.options].forEach(option => {
            option.disabled = (!parent.value && option.dataset.nodeRenderer !== "section") ||
                (parent.selectedOptions[0]?.dataset.tableSection === "true" && option.dataset.nodeRenderer === "section");
        });
        if (types.selectedOptions[0]?.disabled)
            types.value = [...types.options].find(option => !option.disabled)?.value || "";
    };
    const fieldName = field => {
        const base = field.dataset.nodeBase || field.name.split("__")[0];
        const chain = [];
        let current = field;
        while (current) {
            const instance = current.closest("[data-repeat-instance]");
            if (!instance) break;
            const section = instance.closest("section[data-form-node-id]");
            if (!section) break;
            if (section.dataset.repeatSection === "true") chain.unshift(instance.dataset.occurrence || "0");
            current = section.parentElement;
        }
        return base + (chain.length ? "__" + chain.join("_") : "");
    };
    const refresh = () => {
        document.querySelectorAll("[data-table-cell]").forEach(refreshCell);
        document.querySelectorAll("[data-node-parent]").forEach(parent => updateNodeTypes(parent.form));
        document.querySelectorAll('[data-repeat-section="true"] [name^="node-"]').forEach(field => {
            field.name = fieldName(field);
        });
    };
    document.addEventListener("click", event => {
        const tab = event.target.closest("[data-section-tab]");
        if (tab) activateTab(tab);
        const display = event.target.closest("[data-table-cell]")?.querySelector("[data-cell-display]");
        if (!display || display.hidden) return;
        const cell = display.closest("[data-table-cell]");
        const editor = cell.querySelector("[data-cell-editor]");
        display.hidden = true;
        editor.hidden = false;
        if (editor.querySelector("[data-graphql-search]")) {
            const table = cell.closest("[data-section-table]");
            if (table) table.style.overflow = "visible";
        }
        editor.querySelector("input:not([type=hidden]), textarea, select, button")?.focus();
    });
    document.addEventListener("keydown", event => {
        const tab = event.target.closest("[data-section-tab]");
        if (tab && ["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) {
            event.preventDefault();
            const tabs = [...tab.closest("[data-form-tabs]").querySelectorAll("[data-section-tab]")];
            const index = tabs.indexOf(tab);
            const next = event.key === "Home" ? 0 : event.key === "End" ? tabs.length - 1 :
                (index + (event.key === "ArrowRight" ? 1 : -1) + tabs.length) % tabs.length;
            activateTab(tabs[next]);
            tabs[next].focus();
        }
        const cell = event.target.closest("[data-table-cell]");
        if (cell && event.key === "Escape" && cell.querySelector("[data-cell-editor]")) {
            finishCell(cell);
            cell.querySelector("[data-cell-display]").focus();
        }
    });
    document.addEventListener("focusout", event => {
        const cell = event.target.closest("[data-table-cell]");
        if (cell?.querySelector("[data-cell-editor]"))
            setTimeout(() => { if (!cell.contains(document.activeElement)) finishCell(cell); }, 0);
    });
    document.addEventListener("change", event => {
        const cell = event.target.closest("[data-table-cell]");
        if (cell) refreshCell(cell);
        if (event.target.matches("[data-node-parent]")) updateNodeTypes(event.target.form);
        if (event.target.matches("[data-section-visualization]")) {
            const textarea = event.target.form.querySelector('[name="configuration_json"]');
            try {
                const configuration = JSON.parse(textarea.value || "{}");
                if (!configuration || Array.isArray(configuration) || typeof configuration !== "object")
                    throw new Error("Configuration must be a JSON object.");
                configuration.visualization = event.target.value;
                textarea.value = JSON.stringify(configuration, null, 2);
                textarea.setCustomValidity("");
            } catch (error) {
                textarea.setCustomValidity(error.message);
                textarea.reportValidity();
            }
        }
    });
    document.addEventListener("input", event => {
        if (event.target.matches('[name="configuration_json"]')) event.target.setCustomValidity("");
        if (event.target.matches('[name="max_occurs"]')) {
            const selector = event.target.form.querySelector("[data-section-visualization]");
            if (selector) selector.querySelector('[value="table"]').disabled =
                selector.dataset.tableEligible !== "true" || Number(event.target.value) <= 1;
        }
    });
    window.digitalProcessesSections = { refresh, fieldName };
    document.addEventListener("htmx:afterSwap", refresh);
    new MutationObserver(refresh).observe(document.documentElement, { childList: true, subtree: true });
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", refresh);
    else refresh();
})();
