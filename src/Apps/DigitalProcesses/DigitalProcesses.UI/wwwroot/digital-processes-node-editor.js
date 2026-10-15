(() => {
    if (window.digitalProcessesNodeEditor) { window.digitalProcessesNodeEditor.bindAll(); return; }
    const api = "/digital-processes/FormNodeResponse";
    const bind = dialog => {
        if (dialog.dataset.bound) return;
        dialog.dataset.bound = "1";
        let draft = null, selected = null, examples = [], offset = 0, total = 0;
        let generation = 0, previewSequence = 0, sourceSequence = 0, workspaceSequence = 0, timer, busy = false, structuralBusy = false;
        const values = new Map();
        const find = selector => dialog.querySelector(selector);
        const status = (text, error = false) => {
            find("[data-wizard-status]").textContent = text;
            find("[data-wizard-status]").className = error ? "small mt-2 text-danger" : "small mt-2 text-muted";
        };
        const request = async (path, body, html = false) => {
            const response = await fetch(api + path, {
                method: body === undefined ? "GET" : "POST", credentials: "same-origin",
                headers: { Accept: html ? "text/html" : "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
                ...(body === undefined ? {} : { body: JSON.stringify(body) })
            });
            if (!response.ok) {
                const problem = await response.json();
                throw new Error(problem.detail || problem.error || problem.title || "HTTP " + response.status);
            }
            return html ? response.text() : response.json();
        };
        const showError = error => status(error.message, true);
        const loadScript = async name => {
            const src = "/_content/DigitalProcesses.UI/" + name + "?v=" + encodeURIComponent(dialog.dataset.assetVersion);
            if ((name.includes("search") && window.digitalProcessesSearch) ||
                (name.includes("sections") && window.digitalProcessesSections)) return;
            await new Promise((resolve, reject) => {
                const script = document.createElement("script");
                script.src = src; script.onload = resolve;
                script.onerror = () => reject(new Error("Cannot load " + name));
                document.head.append(script);
            });
        };
        const workspaceBody = () => ({
            target_version_id: dialog.dataset.targetVersion, draft, selected_id: selected
        });
        const saveValues = () => {
            find("[data-draft-preview]")?.querySelectorAll('[name^="node-"]').forEach(field =>
                values.set(field.name, field.type === "checkbox" ? field.checked : field.value));
        };
        const applySimulation = () => {
            const root = find("[data-draft-preview]");
            if (!root) return;
            const readonly = find("[data-preview-readonly]")?.checked;
            const disabled = find("[data-preview-disabled]")?.checked;
            const invalid = find("[data-preview-invalid]")?.checked;
            root.querySelectorAll("input,textarea,select").forEach(field => {
                if (field.type === "hidden") return;
                field.readOnly = !!readonly;
                field.disabled = !!disabled || (!!readonly && (field.tagName === "SELECT" || field.type === "checkbox" || field.type === "search"));
                field.classList.toggle("is-invalid", !!invalid);
                field.setAttribute("aria-invalid", String(!!invalid));
            });
            const message = root.querySelector("[data-preview-validation]");
            if (message) { message.hidden = !invalid; message.classList.toggle("d-block", !!invalid); }
        };
        const initializePreview = async () => {
            const normalized = find("[data-draft-state]");
            if (normalized) {
                draft = JSON.parse(normalized.textContent);
                const current = draft.nodes.find(node => node.id === selected);
                const configuration = find("[data-draft-properties]")?.elements.namedItem("configuration_json");
                if (configuration && current) configuration.value = current.configuration_json || "{}";
            }
            await loadScript("digital-processes-sections.js");
            await loadScript("digital-processes-search.js");
            window.digitalProcessesRenumber?.(find("[data-draft-preview]"));
            find("[data-draft-preview]")?.querySelectorAll('[name^="node-"]').forEach(field => {
                if (!values.has(field.name)) return;
                if (field.type === "checkbox") field.checked = values.get(field.name);
                else field.value = values.get(field.name);
                if (field.matches("[data-search-value]")) {
                    const input = field.closest("[data-graphql-search]").querySelector("[data-search-input]");
                    input.value = field.value ? JSON.parse(field.value)?.label || "" : "";
                }
            });
            // Side-effect actions are intentionally unavailable in detached previews.
            find("[data-draft-preview]")?.querySelectorAll("[hx-post], [hx-put], [hx-delete]").forEach(button => button.disabled = true);
            window.digitalProcessesSearch?.bindAll();
            window.digitalProcessesSections?.refresh();
            applySimulation();
        };
        const readProperties = () => {
            if (!draft) return;
            const form = find("[data-draft-properties]");
            if (!form) return;
            const current = draft.nodes.find(node => node.id === selected);
            for (const key of ["key", "label", "description", "configuration_json"])
                current[key] = form.elements.namedItem(key).value;
            for (const key of ["position", "min_occurs", "max_occurs"]) {
                const field = form.elements.namedItem(key);
                if (!field.checkValidity() || field.value === "") throw new Error("Enter valid position and occurrence limits.");
                current[key] = Number(field.value);
            }
            const parent = form.elements.namedItem("parent_id");
            if (parent) current.parent_id = parent.value;
            const configuration = JSON.parse(current.configuration_json || "{}");
            if (!configuration || Array.isArray(configuration) || typeof configuration !== "object")
                throw new Error("Configuration must be a JSON object.");
        };
        const replaceHtml = (host, html, selector) => {
            const parsed = new DOMParser().parseFromString(html, "text/html");
            const content = parsed.querySelector(selector);
            if (!content) throw new Error("The server did not return the expected editor.");
            host.replaceChildren(document.importNode(content, true));
        };
        const workspace = async () => {
            const active = generation, sequence = ++workspaceSequence;
            previewSequence++;
            const html = await request("/draft/workspace", workspaceBody(), true);
            if (active !== generation || sequence !== workspaceSequence || !dialog.open) return;
            replaceHtml(find("[data-wizard-workspace]"), html, "[data-draft-workspace]");
            find("[data-wizard-source]").hidden = true;
            find("[data-wizard-edit]").hidden = false;
            await initializePreview();
            status("Temporary copy. Source and target remain unchanged.");
        };
        const preview = async () => {
            try {
                readProperties(); saveValues();
                const sequence = ++previewSequence, active = generation;
                status("Updating preview...");
                const html = await request("/draft/preview", workspaceBody(), true);
                if (sequence !== previewSequence || active !== generation || !dialog.open) return;
                replaceHtml(find("[data-draft-preview-host]"), html, "[data-draft-preview]");
                await initializePreview();
                status("Preview updated. Test values are not saved with the node.");
            } catch (error) { showError(error); }
        };
        const library = async () => {
            const active = generation, sequence = ++sourceSequence;
            find("[data-library-filter]").hidden = false;
            find("[data-library-paging]").hidden = false;
            const params = new URLSearchParams({ offset, limit: 12 });
            new FormData(find("[data-library-filter]")).forEach((value, key) => { if (value) params.set(key, value); });
            const page = await request("/library?" + params);
            if (active !== generation || sequence !== sourceSequence || !dialog.open) return;
            total = page.total;
            const host = find("[data-wizard-results]");
            host.replaceChildren();
            for (const item of page.items) {
                const card = document.createElement("div");
                card.className = "border rounded p-2 mb-2";
                const title = document.createElement("strong");
                title.textContent = item.node.label || item.node.key;
                const details = document.createElement("div");
                details.className = "small text-muted";
                details.textContent = `${item.type_name} · ${item.renderer_key} · ${item.child_count} children · ${item.node.form_version_id}`;
                const snippet = document.createElement("pre");
                snippet.className = "small text-wrap";
                snippet.textContent = (item.node.configuration_json || "{}").slice(0, 240);
                const button = document.createElement("button");
                button.type = "button"; button.className = "btn btn-sm btn-primary"; button.textContent = "Use copy";
                button.dataset.wizardSourceNode = item.node.id;
                card.append(title, details, snippet, button); host.append(card);
            }
            if (!page.items.length) host.textContent = "No readable nodes match these filters.";
            find("[data-library-previous]").disabled = offset === 0;
            find("[data-library-next]").disabled = offset + page.items.length >= total;
            status(`${total} matching readable nodes`);
        };
        const gallery = async () => {
            const active = generation, sequence = ++sourceSequence;
            const html = await request("/renderer-examples", undefined, true);
            if (active !== generation || sequence !== sourceSequence || !dialog.open) return;
            find("[data-library-filter]").hidden = true;
            find("[data-library-paging]").hidden = true;
            replaceHtml(find("[data-wizard-results]"), html, "[data-renderer-gallery]");
            await loadScript("digital-processes-search.js");
            status("Built-in examples rendered by the actual registered components.");
        };
        const prepare = async body => {
            const active = ++generation;
            previewSequence++; clearTimeout(timer);
            const result = await request("/draft", body);
            if (active !== generation || !dialog.open) return;
            draft = result; selected = draft.root_id; values.clear();
            await workspace();
        };
        const changeStructure = async change => {
            if (structuralBusy) throw new Error("Wait for the current tree change to finish.");
            readProperties(); saveValues(); clearTimeout(timer); previewSequence++;
            const previousDraft = structuredClone(draft), previousSelection = selected;
            structuralBusy = true;
            try { change(); await workspace(); }
            catch (error) { draft = previousDraft; selected = previousSelection; throw error; }
            finally { structuralBusy = false; }
        };
        const discard = () => {
            generation++; previewSequence++; clearTimeout(timer);
            draft = null; selected = null; values.clear();
            find("[data-wizard-workspace]").replaceChildren();
            find("[data-wizard-results]").replaceChildren();
            find("[data-wizard-source]").hidden = false;
            find("[data-wizard-edit]").hidden = true;
            if (dialog.open) dialog.close();
        };
        dialog.addEventListener("cancel", event => { if (busy) event.preventDefault(); else discard(); });
        dialog.addEventListener("close", () => { if (draft) discard(); });
        dialog.addEventListener("submit", event => {
            event.preventDefault();
            if (event.target.matches("[data-library-filter]")) { offset = 0; library().catch(showError); }
        });
        dialog.addEventListener("input", event => {
            if (event.target.closest("[data-draft-properties]")) {
                previewSequence++;
                clearTimeout(timer); timer = setTimeout(preview, 500);
            }
        });
        dialog.addEventListener("change", event => {
            if (event.target.closest("[data-draft-preview]")) saveValues();
            if (event.target.matches("[data-preview-readonly],[data-preview-disabled],[data-preview-invalid]")) applySimulation();
            if (event.target.closest("[data-draft-properties]")) {
                previewSequence++;
                clearTimeout(timer); timer = setTimeout(preview, 50);
            }
        });
        dialog.addEventListener("click", async event => {
            const button = event.target.closest("button");
            if (!button || busy) return;
            try {
                if (button.matches("[data-wizard-cancel]")) { discard(); return; }
                if (button.matches("[data-wizard-library]")) { offset = 0; await library(); }
                if (button.matches("[data-wizard-gallery]")) await gallery();
                if (button.matches("[data-library-next]")) { offset += 12; await library(); }
                if (button.matches("[data-library-previous]")) { offset = Math.max(0, offset - 12); await library(); }
                if (button.dataset.wizardSourceNode) await prepare({ source_node_id: button.dataset.wizardSourceNode, include_children: find("[data-include-children]").checked });
                if (button.dataset.wizardRenderer) await prepare({ node_type_id: button.dataset.wizardRenderer });
                if (button.matches("[data-wizard-back]")) {
                    generation++; previewSequence++; clearTimeout(timer);
                    draft = null; values.clear(); find("[data-wizard-workspace]").replaceChildren();
                    find("[data-wizard-edit]").hidden = true; find("[data-wizard-source]").hidden = false;
                    await library();
                }
                if (button.dataset.draftSelect) {
                    await changeStructure(() => { selected = button.dataset.draftSelect; });
                }
                if (button.dataset.draftRemove) {
                    await changeStructure(() => {
                    const removed = new Set([button.dataset.draftRemove]);
                    for (let index = 0; index < draft.nodes.length; index++)
                        draft.nodes.forEach(node => { if (removed.has(node.parent_id)) removed.add(node.id); });
                    draft.nodes = draft.nodes.filter(node => !removed.has(node.id));
                    selected = draft.root_id;
                    });
                }
                if (button.matches("[data-draft-add]")) {
                    await changeStructure(() => {
                    const example = examples.find(item => item.type.id === find("[data-draft-child-type]").value);
                    const id = crypto.randomUUID();
                    draft.nodes.push({ id, parent_id: selected, key: "node_" + id.replaceAll("-", "").slice(0, 8),
                        node_type_id: example.type.id, label: example.type.name, position: draft.nodes.filter(n => n.parent_id === selected).length,
                        min_occurs: 1, max_occurs: 1, configuration_json: example.configuration_json });
                    selected = id;
                    });
                }
                if (button.matches("[data-refresh-draft-preview]")) { clearTimeout(timer); await preview(); }
                if (button.matches("[data-wizard-insert]") && !busy) {
                    if (structuralBusy) throw new Error("Wait for the current tree change to finish.");
                    readProperties(); clearTimeout(timer); busy = true; button.disabled = true;
                    dialog.setAttribute("aria-busy", "true");
                    dialog.querySelectorAll("[data-wizard-cancel]").forEach(cancel => cancel.disabled = true);
                    status("Inserting the prepared copy. Please wait.");
                    try {
                        const position = Number(find("[data-wizard-position]").value);
                        if (!Number.isInteger(position) || position < 0) throw new Error("Choose a nonnegative insertion position.");
                        await request("/insert-draft", { form_version_id: dialog.dataset.targetVersion,
                            parent_id: find("[data-wizard-target]").value || null, position, draft });
                        discard();
                        location.assign(dialog.dataset.insertionReturnUrl ||
                            "/digital-processes/FormVersionResponse/designer/" + dialog.dataset.targetVersion);
                    } finally {
                        busy = false; button.disabled = false;
                        dialog.querySelectorAll("[data-wizard-cancel]").forEach(cancel => cancel.disabled = false);
                        dialog.removeAttribute("aria-busy");
                    }
                }
            } catch (error) { showError(error); }
        });
        dialog.searchDraft = async (nodeId, pattern, submittedValues) => request("/draft/search", {
            workspace: workspaceBody(), node_id: nodeId, pattern, values: submittedValues
        });
        document.addEventListener("click", async event => {
            const button = event.target.closest("[data-open-node-wizard]");
            if (!button || dialog.open) return;
            discard();
            const parent = button.dataset.insertionParent || dialog.dataset.editorRoot || "";
            if ([...find("[data-wizard-target]").options].some(option => option.value === parent))
                find("[data-wizard-target]").value = parent;
            dialog.showModal();
            try {
                examples = await request("/renderer-examples");
                await library();
            } catch (error) { showError(error); }
        });
        if (dialog.dataset.editorRoot) {
            document.querySelectorAll('form[action*="/designer/"]').forEach(form => {
                const input = document.createElement("input");
                input.type = "hidden"; input.name = "editor_root_id"; input.value = dialog.dataset.editorRoot;
                form.append(input);
            });
        }
    };
    const bindAll = () => document.querySelectorAll("[data-node-wizard]").forEach(bind);
    window.digitalProcessesNodeEditor = { bindAll };
    document.addEventListener("DOMContentLoaded", bindAll);
    document.addEventListener("htmx:afterSwap", bindAll);
    bindAll();
})();
