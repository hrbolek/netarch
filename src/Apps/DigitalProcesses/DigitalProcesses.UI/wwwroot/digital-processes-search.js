(() => {
    if (window.digitalProcessesSearch) {
        window.digitalProcessesSearch.bindAll();
        return;
    }
    const bound = new WeakSet();
    const bind = root => {
        if (bound.has(root)) return;
        const input = root.querySelector("[data-search-input]");
        const value = root.querySelector("[data-search-value]");
        const options = root.querySelector("[data-search-options]");
        const status = root.querySelector("[data-search-status]");
        if (!input || !value || !options || !status) return;
        bound.add(root);

        let timer;
        let controller;
        let requestSequence = 0;
        const collectFormValues = () => {
            const form = root.closest("form");
            if (!form) return [];
            return Array.from(form.querySelectorAll('[name^="node-"]')).map(field => {
                let fieldValue;
                if (field.type === "checkbox") {
                    fieldValue = field.checked;
                } else if (field.matches("[data-search-value]")) {
                    try {
                        fieldValue = field.value ? JSON.parse(field.value) : null;
                    } catch {
                        fieldValue = null;
                    }
                } else if (field.type === "number" && field.value !== "") {
                    fieldValue = Number(field.value);
                } else {
                    fieldValue = field.value;
                }
                return { name: field.name, value: fieldValue };
            });
        };
        input.addEventListener("input", () => {
            clearTimeout(timer);
            controller?.abort();
            const hadSelection = value.value !== "";
            value.value = "";
            if (hadSelection) value.dispatchEvent(new Event("change", { bubbles: true }));
            options.replaceChildren();
            options.hidden = true;
            const sequence = ++requestSequence;

            const pattern = input.value.trim();
            if (pattern.length < 3) {
                status.textContent = pattern ? "Zadejte alespoň 3 znaky." : "";
                return;
            }

            status.textContent = "Vyhledávání za chvíli…";
            timer = setTimeout(async () => {
                controller = new AbortController();
                status.textContent = "Vyhledávám…";
                try {
                    const wizard = root.closest("[data-node-wizard]");
                    const response = wizard?.searchDraft ? null : await fetch("/digital-processes/form-node-search", {
                        method: "POST",
                        credentials: "same-origin",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            nodeId: root.dataset.formNodeId,
                            pattern,
                            values: collectFormValues()
                        }),
                        signal: controller.signal
                    });
                    if (response && !response.ok) {
                        const contentType = response.headers.get("Content-Type") || "";
                        const problem = contentType.includes("json") ? await response.json() : null;
                        throw new Error(problem?.detail || problem?.title || `Vyhledávání selhalo (HTTP ${response.status}).`);
                    }

                    const result = response ? await response.json() :
                        await wizard.searchDraft(root.dataset.formNodeId, pattern, collectFormValues());
                    if (sequence !== requestSequence) return;
                    const choices = result?.data?.choices;
                    if (!Array.isArray(choices)) {
                        throw new Error("GraphQL odpověď neobsahuje pole data.choices.");
                    }
                    options.replaceChildren();
                    for (const item of choices) {
                        if (item?.id === undefined || item?.id === null || typeof item.label !== "string") continue;
                        const button = document.createElement("button");
                        button.type = "button";
                        button.className = "list-group-item list-group-item-action";
                        button.setAttribute("role", "option");
                        button.textContent = item.label;
                        button.addEventListener("click", () => {
                            input.value = item.label;
                            value.value = JSON.stringify({ id: item.id, label: item.label });
                            options.hidden = true;
                            status.textContent = "";
                            value.dispatchEvent(new Event("change", { bubbles: true }));
                        });
                        options.append(button);
                    }
                    options.hidden = options.childElementCount === 0;
                    status.textContent = options.hidden ? "Nebyly nalezeny žádné výsledky." : "";
                } catch (error) {
                    if (error.name === "AbortError" || sequence !== requestSequence) return;
                    options.hidden = true;
                    status.textContent = error.message || "Vyhledávání selhalo.";
                }
            }, 400);
        });

        input.addEventListener("focus", () => {
            if (options.childElementCount > 0) options.hidden = false;
        });
        document.addEventListener("click", event => {
            if (!root.contains(event.target)) options.hidden = true;
        });
    };

    const bindAll = () => document.querySelectorAll("[data-graphql-search]").forEach(bind);
    window.digitalProcessesSearch = { bindAll };
    document.addEventListener("htmx:afterSwap", bindAll);
    new MutationObserver(bindAll).observe(document.documentElement, { childList: true, subtree: true });
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", bindAll);
    else bindAll();
})();
