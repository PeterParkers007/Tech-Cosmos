import { validateAll } from "./validate.js";

const API = "http://127.0.0.1:8765";

const state = {
  online: false,
  meta: null,
  tab: "catalog",
  search: "",
  selectedId: null,
  dirty: { catalog: false, recipes: false, structure: false, templates: {} },
  catalog: null,
  recipes: null,
  structure: null,
  templates: [],
  templateContents: {},
};

const $ = (sel) => document.querySelector(sel);
const itemList = $("#itemList");
const editorForm = $("#editorForm");
const emptyState = $("#emptyState");
const issuesList = $("#issuesList");
const connectionStatus = $("#connectionStatus");
const metaInfo = $("#metaInfo");
const dirtyInfo = $("#dirtyInfo");
const gitInfo = $("#gitInfo");
const btnSaveAll = $("#btnSaveAll");

const CATEGORIES = [
  "Foundation", "Framework", "Component", "Infra", "Runtime", "Module", "Pipeline", "Tool", "OS", "Other",
];

async function api(path, options) {
  const res = await fetch(`${API}${path}`, options);
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.error || res.statusText);
  return data;
}

async function probeServer() {
  try {
    state.meta = await api("/api/meta");
    state.online = true;
    connectionStatus.textContent = "已连接仓库";
    connectionStatus.className = "status-pill status-pill--online";
    metaInfo.textContent = state.meta.hubRoot || "—";
    await refreshGitStatus();
    return true;
  } catch {
    state.online = false;
    state.meta = null;
    connectionStatus.textContent = "未连接 — 请运行 start.ps1";
    connectionStatus.className = "status-pill status-pill--offline";
    metaInfo.textContent = "在 Hub 仓库的 Tools/HubStudio 目录启动 python server.py";
    gitInfo.textContent = "";
    return false;
  }
}

async function refreshGitStatus() {
  if (!state.online) return;
  try {
    const data = await api("/api/git-status");
    if (!data.isGitRepo) {
      gitInfo.textContent = "（当前目录不是 git 仓库）";
      return;
    }
    if (!data.changed?.length) {
      gitInfo.textContent = "Data/ 无未提交变更";
      return;
    }
    gitInfo.textContent = `待提交 ${data.changed.length} 项 — git add Data/ && git commit`;
  } catch {
    gitInfo.textContent = "";
  }
}

async function loadAll() {
  if (!state.online) return;
  const [catalog, recipes, structure, tpl] = await Promise.all([
    api("/api/data/catalog"),
    api("/api/data/recipes"),
    api("/api/data/structure"),
    api("/api/templates"),
  ]);
  state.catalog = catalog;
  state.recipes = recipes;
  state.structure = structure;
  state.templates = tpl.templates || [];
  state.templateContents = {};
  state.dirty = { catalog: false, recipes: false, structure: false, templates: {} };
  state.selectedId = null;
  await refreshGitStatus();
  render();
}

function markDirty(key, templateName) {
  if (key === "templates" && templateName) {
    state.dirty.templates[templateName] = true;
  } else {
    state.dirty[key] = true;
  }
  updateDirtyUi();
}

function updateDirtyUi() {
  const any =
    state.dirty.catalog ||
    state.dirty.recipes ||
    state.dirty.structure ||
    Object.keys(state.dirty.templates).length > 0;
  btnSaveAll.disabled = !any || !state.online;
  dirtyInfo.textContent = any ? "● 有未保存的修改" : "";
}

function renderIssues() {
  const issues = validateAll(state);
  issuesList.innerHTML = issues.length
    ? issues
        .map(
          (i) =>
            `<li class="level-${i.level}"><strong>${i.scope}</strong> ${escapeHtml(i.message)}</li>`
        )
        .join("")
    : '<li style="color:var(--success)">无问题</li>';
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function matchesSearch(text) {
  const q = state.search.trim().toLowerCase();
  if (!q) return true;
  return (text || "").toLowerCase().includes(q);
}

function getItemsForTab() {
  switch (state.tab) {
    case "catalog":
      return (state.catalog?.packages || []).map((p) => ({
        id: p.id,
        title: p.displayName || p.id,
        sub: p.id,
        raw: p,
      }));
    case "recipes":
      return (state.recipes?.recipes || []).map((r) => ({
        id: r.id,
        title: r.displayName || r.id,
        sub: r.template,
        raw: r,
      }));
    case "structure":
      return (state.structure?.presets || []).map((p) => ({
        id: p.id,
        title: p.displayName || p.id,
        sub: p.root,
        raw: p,
      }));
    case "templates":
      return (state.templates || []).map((t) => ({
        id: t.name,
        title: t.name,
        sub: t.fileName,
        raw: t,
      }));
    default:
      return [];
  }
}

function renderList() {
  const items = getItemsForTab().filter(
    (i) => matchesSearch(`${i.title} ${i.sub} ${i.id}`)
  );

  itemList.innerHTML = items
    .map(
      (item) => `
    <li class="${item.id === state.selectedId ? "selected" : ""}" data-id="${escapeAttr(item.id)}">
      <div style="flex:1;min-width:0">
        <div class="item-title">${escapeHtml(item.title)}</div>
        <div class="item-sub">${escapeHtml(item.sub || "")}</div>
      </div>
      <div class="item-actions">
        <button type="button" class="btn btn-sm btn-ghost" data-move="up" title="上移">↑</button>
        <button type="button" class="btn btn-sm btn-ghost" data-move="down" title="下移">↓</button>
        <button type="button" class="btn btn-sm btn-danger" data-delete title="删除">×</button>
      </div>
    </li>`
    )
    .join("");

  itemList.querySelectorAll("li").forEach((li) => {
    const id = li.dataset.id;
    li.addEventListener("click", (e) => {
      if (e.target.closest("[data-delete],[data-move]")) return;
      state.selectedId = id;
      render();
    });
    li.querySelector("[data-delete]")?.addEventListener("click", (e) => {
      e.stopPropagation();
      deleteItem(id);
    });
    li.querySelector('[data-move="up"]')?.addEventListener("click", (e) => {
      e.stopPropagation();
      moveItem(id, -1);
    });
    li.querySelector('[data-move="down"]')?.addEventListener("click", (e) => {
      e.stopPropagation();
      moveItem(id, 1);
    });
  });
}

function escapeAttr(s) {
  return String(s).replace(/"/g, "&quot;");
}

function arrayForTab() {
  switch (state.tab) {
    case "catalog":
      return state.catalog.packages;
    case "recipes":
      return state.recipes.recipes;
    case "structure":
      return state.structure.presets;
    default:
      return null;
  }
}

function moveItem(id, delta) {
  const arr = arrayForTab();
  if (!arr) return;
  const idx = arr.findIndex((x) => x.id === id);
  if (idx < 0) return;
  const next = idx + delta;
  if (next < 0 || next >= arr.length) return;
  [arr[idx], arr[next]] = [arr[next], arr[idx]];
  markDirty(state.tab === "catalog" ? "catalog" : state.tab === "recipes" ? "recipes" : "structure");
  render();
}

function deleteItem(id) {
  if (!confirm(`确定删除「${id}」？`)) return;
  if (state.tab === "templates") return;
  const arr = arrayForTab();
  const idx = arr.findIndex((x) => x.id === id);
  if (idx >= 0) arr.splice(idx, 1);
  if (state.selectedId === id) state.selectedId = null;
  markDirty(state.tab === "catalog" ? "catalog" : state.tab === "recipes" ? "recipes" : "structure");
  render();
}

function addItem() {
  if (state.tab === "catalog") {
    const p = {
      id: "com.example.newpackage",
      folder: "Tech-Cosmos.Example.New",
      displayName: "新框架",
      category: "Framework",
      description: "",
      gitUrl: "",
    };
    state.catalog.packages.push(p);
    state.selectedId = p.id;
    markDirty("catalog");
  } else if (state.tab === "recipes") {
    const r = {
      id: "new-recipe",
      displayName: "新胶水 Recipe",
      description: "",
      requires: [],
      dependsOnRecipes: [],
      template: "NewTemplate",
      outputFile: "",
      doc: "",
      needsHeroType: false,
    };
    state.recipes.recipes.push(r);
    state.selectedId = r.id;
    markDirty("recipes");
  } else if (state.tab === "structure") {
    const p = {
      id: "new-preset",
      displayName: "新预设",
      description: "",
      root: "Assets/_Game",
      folders: ["Scripts", "Generated"],
      extraRoots: [],
    };
    state.structure.presets.push(p);
    state.selectedId = p.id;
    markDirty("structure");
  } else if (state.tab === "templates") {
    const name = prompt("新模板名称（不含 .txt）：", "NewTemplate");
    if (!name?.trim()) return;
    const trimmed = name.trim();
    if (state.templates.some((t) => t.name === trimmed)) {
      alert("模板已存在");
      return;
    }
    state.templates.push({ name: trimmed, fileName: `${trimmed}.txt`, size: 0 });
    state.templateContents[trimmed] = "// {{HERO_TYPE}} {{HERO_NAMESPACE}}\n";
    state.selectedId = trimmed;
    markDirty("templates", trimmed);
  }
  render();
}

function field(label, id, value, opts = {}) {
  const { type = "text", hint = "", rows = 3, mono = false } = opts;
  if (type === "textarea") {
    return `
      <div class="field">
        <label for="${id}">${label}</label>
        <textarea id="${id}" name="${id}" rows="${rows}" class="${mono ? "mono" : ""}">${escapeHtml(value || "")}</textarea>
        ${hint ? `<div class="field-hint">${hint}</div>` : ""}
      </div>`;
  }
  return `
    <div class="field">
      <label for="${id}">${label}</label>
      <input type="${type}" id="${id}" name="${id}" value="${escapeAttr(value || "")}" />
      ${hint ? `<div class="field-hint">${hint}</div>` : ""}
    </div>`;
}

function chipEditor(label, id, values, options) {
  const chips = (values || [])
    .map(
      (v, i) =>
        `<span class="chip" data-idx="${i}">${escapeHtml(v)} <button type="button" data-remove="${i}">×</button></span>`
    )
    .join("");
  const opts = (options || [])
    .map((o) => `<option value="${escapeAttr(o)}">${escapeHtml(o)}</option>`)
    .join("");
  return `
    <div class="field" data-chip-field="${id}">
      <label>${label}</label>
      <div class="chip-list" id="${id}-chips">${chips}</div>
      <div class="chip-add">
        <select id="${id}-select"><option value="">从列表添加…</option>${opts}</select>
        <input type="text" id="${id}-input" placeholder="或输入自定义 ID" />
        <button type="button" class="btn btn-sm" id="${id}-add">添加</button>
      </div>
    </div>`;
}

function linesField(label, id, lines) {
  return field(label, id, (lines || []).join("\n"), {
    type: "textarea",
    rows: 12,
    hint: "每行一个相对路径",
  });
}

async function renderEditor() {
  if (!state.selectedId) {
    editorForm.classList.add("hidden");
    emptyState.classList.remove("hidden");
    return;
  }

  emptyState.classList.add("hidden");
  editorForm.classList.remove("hidden");

  if (state.tab === "catalog") {
    const p = state.catalog.packages.find((x) => x.id === state.selectedId);
    if (!p) return;
    const cats = [...new Set([...CATEGORIES, p.category].filter(Boolean))]
      .map((c) => `<option value="${escapeAttr(c)}" ${c === p.category ? "selected" : ""}>${escapeHtml(c)}</option>`)
      .join("");
    editorForm.innerHTML = `
      ${field("包 ID", "id", p.id, { hint: "UPM package name，如 com.techcosmos.core" })}
      ${field("目录 folder", "folder", p.folder, { hint: "frameworkRoot 下的文件夹名" })}
      ${field("显示名称", "displayName", p.displayName)}
      <div class="field"><label for="category">分类</label><select id="category" name="category">${cats}</select></div>
      ${field("描述", "description", p.description, { type: "textarea", rows: 3 })}
      ${field("Git URL（可选）", "gitUrl", p.gitUrl, { hint: "留空表示仅本地/Assets 嵌入" })}
      <div class="form-actions">
        <button type="button" class="btn btn-primary" id="applyBtn">应用修改</button>
      </div>`;
    bindCatalogForm(p);
  } else if (state.tab === "recipes") {
    const r = state.recipes.recipes.find((x) => x.id === state.selectedId);
    if (!r) return;
    const pkgIds = (state.catalog?.packages || []).map((p) => p.id).filter(Boolean);
    const recipeIds = (state.recipes.recipes || []).map((x) => x.id).filter((id) => id !== r.id);
    const tplNames = (state.templates || []).map((t) => t.name);
    const tplOpts = tplNames
      .map((n) => `<option value="${escapeAttr(n)}" ${n === r.template ? "selected" : ""}>${escapeHtml(n)}</option>`)
      .join("");
    editorForm.innerHTML = `
      ${field("Recipe ID", "id", r.id)}
      ${field("显示名称", "displayName", r.displayName)}
      ${field("简短描述", "description", r.description, { type: "textarea", rows: 2 })}
      <div class="field"><label for="template">模板 template</label>
        <select id="template" name="template">${tplOpts}<option value="__custom">自定义…</option></select>
        <input type="text" id="templateCustom" class="hidden" placeholder="模板基名（对应 Data/Templates/xxx.txt）" value="${escapeAttr(r.template)}" />
      </div>
      ${field("输出文件名（可选）", "outputFile", r.outputFile, { hint: "留空则使用 Template 映射或 Template.g.cs" })}
      ${chipEditor("依赖包 requires", "requires", r.requires, pkgIds)}
      ${chipEditor("依赖 Recipe dependsOnRecipes", "dependsOnRecipes", r.dependsOnRecipes, recipeIds)}
      <div class="checkbox-field">
        <input type="checkbox" id="needsHeroType" ${r.needsHeroType ? "checked" : ""} />
        <label for="needsHeroType">需要 Hero 类型配置（needsHeroType）</label>
      </div>
      ${field("Hub 详情文档 doc", "doc", r.doc, { type: "textarea", rows: 8, hint: "Markdown 纯文本，显示在 Unity Hub 胶水详情页" })}
      <div class="field-hint">全局输出目录：${escapeHtml(state.recipes.outputRoot)}（在「保存全部」前可于 JSON 根级编辑）</div>
      ${field("outputRoot", "outputRoot", state.recipes.outputRoot)}
      <div class="form-actions">
        <button type="button" class="btn btn-primary" id="applyBtn">应用修改</button>
        <button type="button" class="btn btn-ghost" id="openTemplateBtn">编辑模板文件</button>
      </div>`;
    bindRecipeForm(r);
  } else if (state.tab === "structure") {
    const p = state.structure.presets.find((x) => x.id === state.selectedId);
    if (!p) return;
    const extraHtml = (p.extraRoots || [])
      .map(
        (er, i) => `
      <div class="extra-root-block" data-extra-idx="${i}">
        ${field(`扩展根 #${i + 1}`, `extraRoot_${i}`, er.root)}
        ${linesField(`文件夹`, `extraFolders_${i}`, er.folders)}
        <button type="button" class="btn btn-sm btn-danger" data-remove-extra="${i}">移除此扩展根</button>
      </div>`
      )
      .join("");
    editorForm.innerHTML = `
      ${field("Preset ID", "id", p.id)}
      ${field("显示名称", "displayName", p.displayName)}
      ${field("描述", "description", p.description, { type: "textarea", rows: 3 })}
      ${field("默认 root", "root", p.root)}
      ${linesField("folders", "folders", p.folders)}
      <div class="section-title">扩展根 extraRoots</div>
      <div id="extraRoots">${extraHtml}</div>
      <button type="button" class="btn btn-sm btn-accent" id="addExtraRoot">+ 添加扩展根</button>
      ${field("defaultRoot（全局）", "defaultRoot", state.structure.defaultRoot)}
      <div class="form-actions">
        <button type="button" class="btn btn-primary" id="applyBtn">应用修改</button>
      </div>`;
    bindStructureForm(p);
  } else if (state.tab === "templates") {
    const name = state.selectedId;
    let content = state.templateContents[name];
    if (content === undefined && state.online) {
      try {
        const data = await api(`/api/templates/${encodeURIComponent(name)}`);
        content = data.content;
        state.templateContents[name] = content;
      } catch {
        content = "";
      }
    }
    editorForm.innerHTML = `
      <div class="field">
        <label>模板 ${escapeHtml(name)}.txt</label>
        <div class="field-hint">占位符：{{HERO_TYPE}}、{{HERO_NAMESPACE}}</div>
        <textarea id="templateContent" class="mono" rows="24">${escapeHtml(content || "")}</textarea>
      </div>
      <div class="form-actions">
        <button type="button" class="btn btn-primary" id="applyTemplateBtn">应用修改</button>
      </div>`;
    $("#applyTemplateBtn").addEventListener("click", () => {
      state.templateContents[name] = $("#templateContent").value;
      markDirty("templates", name);
    });
  }
}

function bindChipField(fieldId, arr, onChange) {
  const refresh = () => {
    onChange([...arr]);
    render();
  };

  document.getElementById(`${fieldId}-add`)?.addEventListener("click", () => {
    const sel = document.getElementById(`${fieldId}-select`);
    const inp = document.getElementById(`${fieldId}-input`);
    const v = (inp.value || sel.value || "").trim();
    if (!v || arr.includes(v)) return;
    arr.push(v);
    refresh();
  });

  document.querySelectorAll(`#${fieldId}-chips [data-remove]`).forEach((btn) => {
    btn.addEventListener("click", () => {
      const idx = Number(btn.dataset.remove);
      arr.splice(idx, 1);
      refresh();
    });
  });
}

function bindCatalogForm(p) {
  $("#applyBtn").addEventListener("click", () => {
    const oldId = p.id;
    p.id = $("#id").value.trim();
    p.folder = $("#folder").value.trim();
    p.displayName = $("#displayName").value.trim();
    p.category = $("#category").value;
    p.description = $("#description").value.trim();
    p.gitUrl = $("#gitUrl").value.trim();
    if (!p.gitUrl) delete p.gitUrl;
    if (oldId !== p.id) state.selectedId = p.id;
    markDirty("catalog");
    render();
  });
}

function bindRecipeForm(r) {
  const tpl = $("#template");
  const custom = $("#templateCustom");
  const syncTpl = () => {
    if (tpl.value === "__custom") {
      custom.classList.remove("hidden");
    } else {
      custom.classList.add("hidden");
    }
  };
  tpl.addEventListener("change", syncTpl);
  syncTpl();

  bindChipField("requires", r.requires || (r.requires = []), () => markDirty("recipes"));
  bindChipField("dependsOnRecipes", r.dependsOnRecipes || (r.dependsOnRecipes = []), () =>
    markDirty("recipes")
  );

  $("#applyBtn").addEventListener("click", () => {
    const oldId = r.id;
    r.id = $("#id").value.trim();
    r.displayName = $("#displayName").value.trim();
    r.description = $("#description").value.trim();
    r.template = tpl.value === "__custom" ? custom.value.trim() : tpl.value;
    r.outputFile = $("#outputFile").value.trim();
    r.doc = $("#doc").value;
    r.needsHeroType = $("#needsHeroType").checked;
    state.recipes.outputRoot = $("#outputRoot").value.trim();
    if (!r.outputFile) delete r.outputFile;
    if (!r.doc) delete r.doc;
    if (oldId !== r.id) state.selectedId = r.id;
    markDirty("recipes");
    render();
  });

  $("#openTemplateBtn")?.addEventListener("click", () => {
    state.tab = "templates";
    state.selectedId = r.template;
    document.querySelectorAll(".tab").forEach((t) => {
      t.classList.toggle("tab--active", t.dataset.tab === "templates");
    });
    render();
  });
}

function bindStructureForm(p) {
  $("#addExtraRoot").addEventListener("click", () => {
    if (!p.extraRoots) p.extraRoots = [];
    p.extraRoots.push({ root: "Assets/StreamingAssets", folders: ["Config"] });
    markDirty("structure");
    render();
  });

  document.querySelectorAll("[data-remove-extra]").forEach((btn) => {
    btn.addEventListener("click", () => {
      p.extraRoots.splice(Number(btn.dataset.removeExtra), 1);
      markDirty("structure");
      render();
    });
  });

  $("#applyBtn").addEventListener("click", () => {
    const oldId = p.id;
    p.id = $("#id").value.trim();
    p.displayName = $("#displayName").value.trim();
    p.description = $("#description").value.trim();
    p.root = $("#root").value.trim();
    p.folders = $("#folders")
      .value.split("\n")
      .map((s) => s.trim())
      .filter(Boolean);
    state.structure.defaultRoot = $("#defaultRoot").value.trim();
    (p.extraRoots || []).forEach((er, i) => {
      er.root = $(`#extraRoot_${i}`).value.trim();
      er.folders = $(`#extraFolders_${i}`)
        .value.split("\n")
        .map((s) => s.trim())
        .filter(Boolean);
    });
    if (!p.extraRoots?.length) delete p.extraRoots;
    if (oldId !== p.id) state.selectedId = p.id;
    markDirty("structure");
    render();
  });
}

function render() {
  renderList();
  renderIssues();
  updateDirtyUi();
  renderEditor();
}

async function saveAll() {
  if (!state.online) return;
  const issues = validateAll(state).filter((i) => i.level === "error");
  if (issues.length && !confirm(`存在 ${issues.length} 个错误，仍要保存吗？`)) return;

  if (state.dirty.catalog) {
    await api("/api/data/catalog", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(state.catalog),
    });
  }
  if (state.dirty.recipes) {
    await api("/api/data/recipes", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(state.recipes),
    });
  }
  if (state.dirty.structure) {
    await api("/api/data/structure", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(state.structure),
    });
  }

  for (const name of Object.keys(state.dirty.templates)) {
    if (!state.dirty.templates[name]) continue;
    const content = state.templateContents[name] ?? "";
    await api(`/api/templates/${encodeURIComponent(name)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ content }),
    });
  }

  state.dirty = { catalog: false, recipes: false, structure: false, templates: {} };
  await refreshGitStatus();
  render();

  const lines = [
    "已保存到 Data/ 目录。",
    "",
    "在 Hub 仓库根目录执行：",
    "  git add Data/",
    '  git commit -m "hub: 描述本次扩展"',
    "  git push origin main",
  ];
  alert(lines.join("\n"));
}

document.querySelectorAll(".tab").forEach((tab) => {
  tab.addEventListener("click", () => {
    state.tab = tab.dataset.tab;
    state.selectedId = null;
    state.search = "";
    $("#searchInput").value = "";
    document.querySelectorAll(".tab").forEach((t) => t.classList.toggle("tab--active", t === tab));
    render();
  });
});

$("#searchInput").addEventListener("input", (e) => {
  state.search = e.target.value;
  renderList();
});

$("#btnAdd").addEventListener("click", addItem);
$("#btnReload").addEventListener("click", async () => {
  await probeServer();
  await loadAll();
});
$("#btnSaveAll").addEventListener("click", saveAll);

(async function init() {
  await probeServer();
  if (state.online) await loadAll();
  else render();
})();
