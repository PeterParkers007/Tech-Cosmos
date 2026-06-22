import { validateAll } from "./validate.js";
import {
  normalizeCategoryTree,
  getFirstCategoryPath,
  getCategoryOptions,
  addCategoryNode,
  renameCategoryAtPath,
  deleteCategoryAtPath,
  remapItemCategoryPath,
  moveItemsToCategory,
  moveItemInCategory,
  moveCategoryInTree,
  pathBasename,
  pathParent,
  categoryPathExists,
  renderCategoryTreeList,
  PATH_SEP,
} from "./categoryTree.js";

const API = "http://127.0.0.1:8765";

const state = {
  online: false,
  meta: null,
  tab: "catalog",
  search: "",
  selectedId: null,
  dirty: { catalog: false, recipes: false, structure: false, templatesMeta: false, templates: {} },
  catalog: null,
  recipes: null,
  structure: null,
  templatesMeta: null,
  templates: [],
  templateContents: {},
  originalTemplates: new Set(),
  deletedTemplates: new Set(),
};

let editorRenderToken = 0;

const $ = (sel) => document.querySelector(sel);
const itemList = $("#itemList");
const editorForm = $("#editorForm");
const emptyState = $("#emptyState");
const issuesList = $("#issuesList");
const connectionStatus = $("#connectionStatus");
const metaInfo = $("#metaInfo");
const dirtyInfo = $("#dirtyInfo");
const gitInfo = $("#gitInfo");
const pathWarnings = $("#pathWarnings");
const btnSaveAll = $("#btnSaveAll");

function renderPathWarnings(warnings, meta) {
  const blocks = [];
  if (meta?.dataDir) {
    blocks.push(`<strong>保存到此目录：</strong><code>${escapeHtml(meta.dataDir)}</code>`);
  }
  for (const w of warnings || []) {
    blocks.push(escapeHtml(w));
  }
  if (!blocks.length) {
    pathWarnings.classList.add("hidden");
    pathWarnings.innerHTML = "";
    return;
  }
  pathWarnings.classList.remove("hidden");
  pathWarnings.innerHTML = blocks.map((b) => `<div>${b}</div>`).join("");
}

function formatMetaLine(meta) {
  if (!meta) return "—";
  return `Data: ${meta.dataDir}`;
}

const TREE_TABS = new Set(["catalog", "recipes", "structure", "templates"]);
const ITEM_DRAG_MIME = "application/x-hub-item-id";

const DEFAULT_CATALOG_CATEGORIES = [
  "Foundation", "Framework", "Component", "Infra", "Runtime", "Module", "Pipeline", "Tool", "OS", "Other",
];
const DEFAULT_GLUE_CATEGORIES = ["Integration", "Core", "Bridge", "Input", "Other"];
const DEFAULT_STRUCTURE_CATEGORIES = ["Presets", "Other"];
const DEFAULT_TEMPLATE_CATEGORIES = ["Glue", "Project", "Other"];

const OUTPUT_PLACEHOLDER_HINT =
  "{{TEMPLATE}} {{RECIPE_ID}} {{DISPLAY_NAME}} {{HERO_TYPE}} {{HERO_NAMESPACE}} {{PROJECT_NAME}}";

function ensureTemplatesMetaEntries() {
  if (!state.templatesMeta) {
    state.templatesMeta = {
      categories: DEFAULT_TEMPLATE_CATEGORIES.map((name) => ({ name, children: [] })),
      entries: [],
    };
  }
  if (!state.templatesMeta.entries) state.templatesMeta.entries = [];
  if (state.templatesMeta.items && typeof state.templatesMeta.items === "object") {
    for (const [name, value] of Object.entries(state.templatesMeta.items)) {
      if (typeof value === "string") {
        state.templatesMeta.entries.push({ name, category: value, outputFile: "" });
      } else if (value && typeof value === "object") {
        state.templatesMeta.entries.push({
          name,
          category: value.category || "Other",
          outputFile: value.outputFile || "",
        });
      }
    }
    delete state.templatesMeta.items;
  }
}

function getTemplateEntry(name) {
  ensureTemplatesMetaEntries();
  let entry = state.templatesMeta.entries.find((e) => e.name === name);
  if (!entry) {
    entry = { name, category: "Other", outputFile: "" };
    state.templatesMeta.entries.push(entry);
  }
  return entry;
}

function applyOutputPlaceholders(pattern, recipeLike = {}) {
  const displayName = recipeLike.displayName || recipeLike.id || "Output";
  return String(pattern || "")
    .replace(/\{\{TEMPLATE\}\}/g, recipeLike.template || "")
    .replace(/\{\{RECIPE_ID\}\}/g, recipeLike.id || "")
    .replace(/\{\{DISPLAY_NAME\}\}/g, displayName)
    .replace(/\{\{HERO_TYPE\}\}/g, "IntegrationHero")
    .replace(/\{\{HERO_NAMESPACE\}\}/g, "TechCosmos.Game.Integration")
    .replace(/\{\{PROJECT_NAME\}\}/g, "My Game");
}

function resolveRecipeOutputFile(recipe) {
  if (!recipe) return "";
  const pattern =
    recipe.outputFile?.trim() ||
    getTemplateEntry(recipe.template).outputFile?.trim() ||
    `${recipe.template || "Output"}.g.cs`;
  return applyOutputPlaceholders(pattern, recipe);
}

function syncTemplateEntriesFromList() {
  ensureTemplatesMetaEntries();
  for (const t of state.templates) {
    const entry = getTemplateEntry(t.name);
    t.category = entry.category || "Other";
    t.outputFile = entry.outputFile || "";
  }
}

function getTabTreeConfig(tab = state.tab) {
  switch (tab) {
    case "catalog":
      return {
        dirtyKey: "catalog",
        getTree: () => state.catalog?.categories,
        setTree: (t) => {
          state.catalog.categories = t;
        },
        getItems: () => state.catalog?.packages || [],
        getItemId: (p) => p.id,
        getItemCategory: (p) => p.category,
        setItemCategory: (p, path) => {
          p.category = path;
        },
        getItemTitle: (p) => p.displayName || p.id,
        getItemSub: (p) => p.id,
        defaultCategories: DEFAULT_CATALOG_CATEGORIES,
        protectedNames: ["Other"],
        showMoveButtons: true,
      };
    case "recipes":
      return {
        dirtyKey: "recipes",
        getTree: () => state.recipes?.categories,
        setTree: (t) => {
          state.recipes.categories = t;
        },
        getItems: () => state.recipes?.recipes || [],
        getItemId: (r) => r.id,
        getItemCategory: (r) => r.category,
        setItemCategory: (r, path) => {
          r.category = path;
        },
        getItemTitle: (r) => r.displayName || r.id,
        getItemSub: (r) => r.template || "",
        defaultCategories: DEFAULT_GLUE_CATEGORIES,
        protectedNames: ["Other"],
        showMoveButtons: true,
      };
    case "structure":
      return {
        dirtyKey: "structure",
        getTree: () => state.structure?.categories,
        setTree: (t) => {
          state.structure.categories = t;
        },
        getItems: () => state.structure?.presets || [],
        getItemId: (p) => p.id,
        getItemCategory: (p) => p.category,
        setItemCategory: (p, path) => {
          p.category = path;
        },
        getItemTitle: (p) => p.displayName || p.id,
        getItemSub: (p) => p.root || "",
        defaultCategories: DEFAULT_STRUCTURE_CATEGORIES,
        protectedNames: ["Other"],
        showMoveButtons: true,
      };
    case "templates":
      return {
        dirtyKey: "templatesMeta",
        getTree: () => state.templatesMeta?.categories,
        setTree: (t) => {
          state.templatesMeta.categories = t;
        },
        getItems: () => state.templates || [],
        getItemId: (t) => t.name,
        getItemCategory: (t) => getTemplateEntry(t.name).category,
        setItemCategory: (t, path) => {
          getTemplateEntry(t.name).category = path;
          t.category = path;
        },
        getItemTitle: (t) => t.name,
        getItemSub: (t) => {
          const out = getTemplateEntry(t.name).outputFile?.trim();
          return out ? `→ ${out}` : t.fileName || "";
        },
        defaultCategories: DEFAULT_TEMPLATE_CATEGORIES,
        protectedNames: ["Other"],
        showMoveButtons: false,
      };
    default:
      return null;
  }
}

function ensureTabCategories(tab = state.tab) {
  const cfg = getTabTreeConfig(tab);
  if (!cfg) return;
  const tree = normalizeCategoryTree(cfg.getTree(), cfg.defaultCategories);
  cfg.setTree(tree);
  const fallback = getFirstCategoryPath(tree);
  for (const item of cfg.getItems()) {
    if (!cfg.getItemCategory(item)?.trim()) cfg.setItemCategory(item, fallback);
  }
  if (tab === "templates") syncTemplateEntriesFromList();
}

function ensureAllTabCategories() {
  for (const tab of TREE_TABS) ensureTabCategories(tab);
}

function tabCategoryOptions(tab, current) {
  const cfg = getTabTreeConfig(tab);
  if (!cfg) return [];
  ensureTabCategories(tab);
  return getCategoryOptions(cfg.getTree(), current);
}

function promptNewCategory(parentPath = null) {
  const cfg = getTabTreeConfig();
  if (!cfg) return;
  ensureTabCategories();
  const tree = cfg.getTree();
  const label = parentPath ? `在「${parentPath}」下新建子分类：` : "新分类名称：";
  const name = prompt(label, "");
  if (!name?.trim()) return;
  if (!addCategoryNode(tree, parentPath, name.trim())) {
    alert("该分类已存在");
    return;
  }
  markDirty(cfg.dirtyKey);
  render();
  const fullPath = parentPath ? `${parentPath}${PATH_SEP}${name.trim()}` : name.trim();
  showStatus(`已创建分类「${fullPath}」— 将条目拖到该分类下即可`);
}

function renameTabCategory(path) {
  const cfg = getTabTreeConfig();
  if (!cfg) return;
  const next = prompt(`重命名分类「${path}」：`, pathBasename(path));
  if (!next?.trim()) return;
  const result = renameCategoryAtPath(cfg.getTree(), path, next.trim());
  if (!result) {
    alert("该分类名已存在或无法重命名");
    return;
  }
  remapItemCategoryPath(result.oldPath, result.newPath, cfg.getItems(), cfg.getItemCategory, cfg.setItemCategory);
  if (state.tab === "templates") syncTemplatesMetaItems();
  markDirty(cfg.dirtyKey);
  render();
  showStatus(`分类已重命名为「${result.newPath}」`);
}

function deleteTabCategory(path) {
  const cfg = getTabTreeConfig();
  if (!cfg) return;
  if (cfg.protectedNames.includes(path) || cfg.protectedNames.includes(pathBasename(path))) {
    alert(`「${pathBasename(path)}」为默认分类，不能删除`);
    return;
  }
  const count = cfg.getItems().filter((item) => {
    const cat = (cfg.getItemCategory(item) || "").trim();
    return cat === path || cat.startsWith(`${path}${PATH_SEP}`);
  }).length;
  const fallback = cfg.protectedNames.find((n) => n === "Other") || getFirstCategoryPath(cfg.getTree());
  if (!confirm(`删除分类「${path}」？${count ? ` 其中 ${count} 条条目将移到 ${fallback}。` : ""}`)) return;
  moveItemsToCategory(cfg.getItems(), path, fallback, cfg.getItemCategory, cfg.setItemCategory, fallback);
  if (!deleteCategoryAtPath(cfg.getTree(), path, cfg.protectedNames)) return;
  if (state.tab === "templates") syncTemplatesMetaItems();
  markDirty(cfg.dirtyKey);
  render();
}

function moveTabItemToCategory(itemId, categoryPath) {
  const cfg = getTabTreeConfig();
  if (!cfg) return;
  const item = cfg.getItems().find((x) => cfg.getItemId(x) === itemId);
  if (!item) return;
  const cat = categoryPath?.trim() || "Other";
  if ((cfg.getItemCategory(item) || "").trim() === cat) return;
  cfg.setItemCategory(item, cat);
  if (!categoryPathExists(cfg.getTree(), cat)) addCategoryNode(cfg.getTree(), pathParent(cat), pathBasename(cat));
  if (state.tab === "templates") syncTemplatesMetaItems();
  markDirty(cfg.dirtyKey);
  render();
  showStatus(`已将「${cfg.getItemTitle(item)}」移到「${cat}」`);
}

function moveTabItemInCategory(itemId, delta) {
  const cfg = getTabTreeConfig();
  if (!cfg) return;
  const items = cfg.getItems();
  moveItemInCategory(items, itemId, delta, cfg.getItemId, cfg.getItemCategory);
  markDirty(cfg.dirtyKey);
  render();
}

function moveTabCategory(categoryPath, delta) {
  const cfg = getTabTreeConfig();
  if (!cfg) return;
  ensureTabCategories();
  if (!moveCategoryInTree(cfg.getTree(), categoryPath, delta)) return;
  markDirty(cfg.dirtyKey);
  render();
}

function syncTemplatesMetaItems() {
  syncTemplateEntriesFromList();
}

const TREE_EMPTY_HINT =
  "左侧按<strong>分类树</strong>组织：<br>• 点「+ 分类」或分类旁的「+」创建子分类<br>• 分类/条目的 ↑↓ 调整同级顺序<br>• 拖动 ⠿ 将条目拖到目标分类下<br>• 点击条目编辑详情<br>• 完成后点「保存全部」";

async function api(path, options) {
  const res = await fetch(`${API}${path}`, options);
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const detail = data.error || res.statusText || `HTTP ${res.status}`;
    throw new Error(`${path} → ${detail}`);
  }
  return data;
}

async function probeServer() {
  try {
    state.meta = await api("/api/meta");
    state.online = true;
    connectionStatus.textContent = "已连接";
    connectionStatus.className = "status-pill status-pill--online";
    metaInfo.textContent = formatMetaLine(state.meta);
    renderPathWarnings(state.meta.warnings, state.meta);
    await refreshGitStatus();
    return true;
  } catch {
    state.online = false;
    state.meta = null;
    connectionStatus.textContent = "未连接 — 请运行 start.ps1";
    connectionStatus.className = "status-pill status-pill--offline";
    metaInfo.textContent = "在 Tools/HubStudio 目录运行: python server.py";
    gitInfo.textContent = "";
    renderPathWarnings([]);
    return false;
  }
}

async function refreshGitStatus() {
  if (!state.online) return;
  try {
    const data = await api("/api/git-status");
    renderPathWarnings(data.warnings || state.meta?.warnings, state.meta);
    if (!data.isGitRepo) {
      gitInfo.textContent = "未检测到 Git 仓库";
      return;
    }
    const prefix = data.gitDataPrefix || "Data/";
    if (!data.changed?.length) {
      gitInfo.textContent = `${prefix} 无未提交变更`;
      return;
    }
    gitInfo.textContent = `待提交 ${data.changed.length} 项 → git add ${prefix}`;
  } catch {
    gitInfo.textContent = "";
  }
}

async function loadAll() {
  if (!state.online) return;
  const [catalog, recipes, structure, tpl, templatesMeta] = await Promise.all([
    api("/api/data/catalog"),
    api("/api/data/recipes"),
    api("/api/data/structure"),
    api("/api/templates"),
    api("/api/data/templatesMeta").catch(() => null),
  ]);
  state.catalog = catalog;
  state.recipes = recipes;
  state.structure = structure;
  state.templatesMeta = templatesMeta || {
    categories: DEFAULT_TEMPLATE_CATEGORIES.map((name) => ({ name, children: [] })),
    entries: [],
  };
  state.templates = tpl.templates || [];
  ensureTemplatesMetaEntries();
  syncTemplateEntriesFromList();
  ensureAllTabCategories();
  state.templateContents = {};
  state.originalTemplates = new Set(state.templates.map((t) => t.name));
  state.deletedTemplates = new Set();
  state.dirty = { catalog: false, recipes: false, structure: false, templatesMeta: false, templates: {} };
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

function hasUnsavedChanges() {
  return (
    state.dirty.catalog ||
    state.dirty.recipes ||
    state.dirty.structure ||
    state.dirty.templatesMeta ||
    Object.keys(state.dirty.templates).length > 0 ||
    state.deletedTemplates.size > 0
  );
}

function updateDirtyUi() {
  const any = hasUnsavedChanges();
  btnSaveAll.disabled = !any || !state.online;
  dirtyInfo.style.color = "";
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

function renderList() {
  const cfg = getTabTreeConfig();
  if (!cfg) {
    itemList.classList.remove("category-tree");
    itemList.innerHTML = "";
    return;
  }

  ensureTabCategories();
  renderCategoryTreeList(itemList, {
    tree: cfg.getTree(),
    items: cfg.getItems(),
    search: state.search,
    selectedId: state.selectedId,
    getItemId: cfg.getItemId,
    getItemCategory: cfg.getItemCategory,
    getItemTitle: cfg.getItemTitle,
    getItemSub: cfg.getItemSub,
    dragMime: ITEM_DRAG_MIME,
    showMoveButtons: cfg.showMoveButtons,
    onSelect: (id) => {
      state.selectedId = id;
      render();
    },
    onDelete: (id) => {
      void deleteItem(id);
    },
    onMoveInCategory: (id, delta) => moveTabItemInCategory(id, delta),
    onMoveToCategory: (id, path) => moveTabItemToCategory(id, path),
    onMoveCategory: (path, delta) => moveTabCategory(path, delta),
    onAddSubCategory: (path) => promptNewCategory(path),
    onRenameCategory: (path) => renameTabCategory(path),
    onDeleteCategory: (path) => deleteTabCategory(path),
    onAddRootCategory: () => promptNewCategory(null),
  });
}

function escapeAttr(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

function showStatus(message, isError = false) {
  dirtyInfo.textContent = message;
  dirtyInfo.style.color = isError ? "var(--danger)" : "var(--success)";
  if (message) {
    window.clearTimeout(showStatus._timer);
    showStatus._timer = window.setTimeout(() => {
      if (dirtyInfo.textContent === message) updateDirtyUi();
    }, 4000);
  }
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

async function deleteItem(id) {
  const trimmedId = String(id || "").trim();
  if (!trimmedId) return;

  const isTemplate = state.tab === "templates";
  const confirmMsg = isTemplate
    ? `确定删除模板「${trimmedId}」？\n\n将立即从左侧列表移除，并删除磁盘上的 .txt 文件。`
    : `确定删除「${trimmedId}」？\n\n将从列表移除，需点击「保存全部」才会写入 JSON。`;
  if (!confirm(confirmMsg)) return;

  if (isTemplate) {
    const usedBy = (state.recipes?.recipes || []).filter((r) => r.template === trimmedId);
    if (usedBy.length && !confirm(`有 ${usedBy.length} 个 Recipe 仍引用此模板，确定删除？`)) return;

    const idx = state.templates.findIndex((t) => t.name === trimmedId);
    if (idx < 0) {
      showStatus(`未找到模板「${trimmedId}」`, true);
      return;
    }

    const wasOnDisk = state.originalTemplates.has(trimmedId);
    state.templates.splice(idx, 1);
    delete state.templateContents[trimmedId];
    delete state.dirty.templates[trimmedId];
    if (state.templatesMeta?.entries) {
      state.templatesMeta.entries = state.templatesMeta.entries.filter((e) => e.name !== trimmedId);
    }
    if (state.selectedId === trimmedId) state.selectedId = null;

    render();

    if (state.online && wasOnDisk) {
      try {
        await api(`/api/templates/${encodeURIComponent(trimmedId)}`, { method: "DELETE" });
        state.originalTemplates.delete(trimmedId);
        state.deletedTemplates.delete(trimmedId);
        showStatus(`已删除模板 ${trimmedId}.txt`);
        await refreshGitStatus();
      } catch (err) {
        state.deletedTemplates.add(trimmedId);
        showStatus(`列表已更新，磁盘删除失败：${err.message}`, true);
        updateDirtyUi();
      }
      return;
    }

    showStatus(`已从列表移除模板「${trimmedId}」`);
    updateDirtyUi();
    return;
  }

  const arr = arrayForTab();
  if (!arr) return;
  const idx = arr.findIndex((x) => x.id === trimmedId);
  if (idx < 0) {
    showStatus(`未找到条目「${trimmedId}」`, true);
    return;
  }
  arr.splice(idx, 1);
  if (state.selectedId === trimmedId) state.selectedId = null;
  markDirty(state.tab === "catalog" ? "catalog" : state.tab === "recipes" ? "recipes" : "structure");
  render();
  showStatus(`已从列表移除「${trimmedId}」，请点击「保存全部」`);
}

function addItem() {
  if (state.tab === "catalog") {
    ensureTabCategories("catalog");
    const p = {
      id: "com.example.newpackage",
      folder: "Tech-Cosmos.Example.New",
      displayName: "新框架",
      category: getFirstCategoryPath(state.catalog.categories),
      description: "",
      gitUrl: "",
      dependsOn: [],
    };
    state.catalog.packages.push(p);
    state.selectedId = p.id;
    markDirty("catalog");
  } else if (state.tab === "recipes") {
    ensureTabCategories("recipes");
    const r = {
      id: "new-recipe",
      displayName: "新胶水 Recipe",
      category: getFirstCategoryPath(state.recipes.categories),
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
    ensureTabCategories("structure");
    const p = {
      id: "new-preset",
      displayName: "新预设",
      category: getFirstCategoryPath(state.structure.categories),
      description: "",
      root: "Assets/_Game",
      folders: ["Scripts", "Generated"],
      extraRoots: [],
    };
    state.structure.presets.push(p);
    state.selectedId = p.id;
    markDirty("structure");
  } else if (state.tab === "templates") {
    ensureTabCategories("templates");
    const name = prompt("新模板名称（不含 .txt）：", "NewTemplate");
    if (!name?.trim()) return;
    const trimmed = name.trim();
    if (state.templates.some((t) => t.name === trimmed)) {
      alert("模板已存在");
      return;
    }
    const category = getFirstCategoryPath(state.templatesMeta.categories);
    const entry = getTemplateEntry(trimmed);
    entry.category = category;
    entry.outputFile = `${trimmed}.g.cs`;
    state.templates.push({
      name: trimmed,
      fileName: `${trimmed}.txt`,
      size: 0,
      category,
      outputFile: entry.outputFile,
    });
    state.templateContents[trimmed] = "// {{HERO_TYPE}} {{HERO_NAMESPACE}}\n";
    state.selectedId = trimmed;
    markDirty("templates", trimmed);
    markDirty("templatesMeta");
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
  const token = ++editorRenderToken;

  if (!state.selectedId) {
    editorForm.classList.add("hidden");
    emptyState.classList.remove("hidden");
    if (TREE_TABS.has(state.tab)) {
      emptyState.querySelector("h2").textContent = "选择或新建一项";
      emptyState.querySelector("p").innerHTML = TREE_EMPTY_HINT;
    } else {
      emptyState.querySelector("h2").textContent = "选择或新建一项";
      emptyState.querySelector("p").textContent = "选择左侧条目编辑。保存写入本仓库 Data/。";
    }
    return;
  }

  emptyState.classList.add("hidden");
  editorForm.classList.remove("hidden");

  if (state.tab === "catalog") {
    const p = state.catalog.packages.find((x) => x.id === state.selectedId);
    if (!p) return;
    const catOpts = tabCategoryOptions("catalog", p.category)
      .map((c) => `<option value="${escapeAttr(c)}" ${c === p.category ? "selected" : ""}>${escapeHtml(c)}</option>`)
      .join("");
    editorForm.innerHTML = `
      ${field("包 ID", "id", p.id, { hint: "UPM package name，如 com.techcosmos.core" })}
      ${field("目录 folder", "folder", p.folder, { hint: "frameworkRoot 下的文件夹名" })}
      ${field("显示名称", "displayName", p.displayName)}
      <div class="field"><label for="category">分类</label><select id="category" name="category">${catOpts}</select></div>
      ${field("描述", "description", p.description, { type: "textarea", rows: 3 })}
      ${field("Git URL（可选）", "gitUrl", p.gitUrl, { hint: "留空表示仅本地/Assets 嵌入" })}
      ${chipEditor("依赖包 dependsOn", "dependsOn", p.dependsOn || [], (state.catalog?.packages || []).map((x) => x.id).filter((id) => id !== p.id))}
      <div class="form-actions">
        <button type="button" class="btn btn-primary" id="applyBtn">应用修改</button>
      </div>`;
    bindCatalogForm(p);
  } else if (state.tab === "recipes") {
    ensureTabCategories("recipes");
    const r = state.recipes.recipes.find((x) => x.id === state.selectedId);
    if (!r) return;
    const pkgIds = (state.catalog?.packages || []).map((p) => p.id).filter(Boolean);
    const recipeIds = (state.recipes.recipes || []).map((x) => x.id).filter((id) => id !== r.id);
    const tplNames = (state.templates || []).map((t) => t.name);
    const tplOpts = tplNames
      .map((n) => `<option value="${escapeAttr(n)}" ${n === r.template ? "selected" : ""}>${escapeHtml(n)}</option>`)
      .join("");
    const catOpts = tabCategoryOptions("recipes", r.category)
      .map((c) => `<option value="${escapeAttr(c)}" ${c === r.category ? "selected" : ""}>${escapeHtml(c)}</option>`)
      .join("");
    const resolvedOutput = resolveRecipeOutputFile(r);
    editorForm.innerHTML = `
      ${field("Recipe ID", "id", r.id)}
      ${field("显示名称", "displayName", r.displayName)}
      <div class="field"><label for="category">分类 category</label><select id="category" name="category">${catOpts}</select></div>
      ${field("简短描述", "description", r.description, { type: "textarea", rows: 2 })}
      <div class="field"><label for="template">模板 template</label>
        <select id="template" name="template">${tplOpts}<option value="__custom">自定义…</option></select>
        <input type="text" id="templateCustom" class="hidden" placeholder="模板基名（对应 Data/Templates/xxx.txt）" value="${escapeAttr(r.template)}" />
      </div>
      ${field("输出文件（可选，覆盖模板默认）", "outputFile", r.outputFile, {
        hint: `留空则用模板默认。支持路径如 Config/data.json。占位符：${OUTPUT_PLACEHOLDER_HINT}。当前解析：${resolvedOutput}`,
      })}
      ${chipEditor("依赖包 requires", "requires", r.requires, pkgIds)}
      ${chipEditor("依赖 Recipe dependsOnRecipes", "dependsOnRecipes", r.dependsOnRecipes, recipeIds)}
      <div class="checkbox-field">
        <input type="checkbox" id="needsHeroType" ${r.needsHeroType ? "checked" : ""} />
        <label for="needsHeroType">需要 Hero 类型配置（needsHeroType）</label>
      </div>
      ${field("详细说明 doc", "doc", r.doc, { type: "textarea", rows: 8, hint: "Markdown 纯文本，可选" })}
      <div class="field-hint">全局输出目录：${escapeHtml(state.recipes.outputRoot)}（在「保存全部」前可于 JSON 根级编辑）</div>
      ${field("outputRoot", "outputRoot", state.recipes.outputRoot)}
      <div class="form-actions">
        <button type="button" class="btn btn-primary" id="applyBtn">应用修改</button>
        <button type="button" class="btn btn-ghost" id="openTemplateBtn">编辑模板文件</button>
      </div>`;
    bindRecipeForm(r);
  } else if (state.tab === "structure") {
    ensureTabCategories("structure");
    const p = state.structure.presets.find((x) => x.id === state.selectedId);
    if (!p) return;
    const catOpts = tabCategoryOptions("structure", p.category)
      .map((c) => `<option value="${escapeAttr(c)}" ${c === p.category ? "selected" : ""}>${escapeHtml(c)}</option>`)
      .join("");
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
      <div class="field"><label for="category">分类 category</label><select id="category" name="category">${catOpts}</select></div>
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
    if (!state.templates.some((t) => t.name === name)) {
      if (token !== editorRenderToken) return;
      editorForm.classList.add("hidden");
      emptyState.classList.remove("hidden");
      return;
    }

    let content = state.templateContents[name];
    if (content === undefined && state.online) {
      try {
        const data = await api(`/api/templates/${encodeURIComponent(name)}`);
        if (token !== editorRenderToken) return;
        content = data.content;
        state.templateContents[name] = content;
      } catch {
        if (token !== editorRenderToken) return;
        content = "";
      }
    }
    if (token !== editorRenderToken) return;

    const entry = getTemplateEntry(name);
    editorForm.innerHTML = `
      ${field("默认输出文件", "templateOutputFile", entry.outputFile, {
        hint: `胶水生成时的默认文件名/路径，如 MyType.g.cs、Config/settings.json、Adapters/{{HERO_TYPE}}.md。占位符：${OUTPUT_PLACEHOLDER_HINT}`,
      })}
      <div class="field">
        <label>模板 ${escapeHtml(name)}.txt</label>
        <div class="field-hint">内容占位符：${OUTPUT_PLACEHOLDER_HINT}</div>
        <textarea id="templateContent" class="mono" rows="24">${escapeHtml(content || "")}</textarea>
      </div>
      <div class="form-actions">
        <button type="button" class="btn btn-primary" id="applyTemplateBtn">应用修改</button>
      </div>`;

    const syncTemplateMeta = () => {
      entry.outputFile = $("#templateOutputFile")?.value?.trim() || "";
      state.templateContents[name] = $("#templateContent").value;
      const t = state.templates.find((x) => x.name === name);
      if (t) t.outputFile = entry.outputFile;
      markDirty("templates", name);
      markDirty("templatesMeta");
    };
    $("#templateOutputFile")?.addEventListener("input", syncTemplateMeta);
    $("#templateContent").addEventListener("input", syncTemplateMeta);
    $("#applyTemplateBtn").addEventListener("click", syncTemplateMeta);
  }
}

function bindChipField(fieldId, arr, onChange, resyncForm, onRemove) {
  const refresh = () => {
    resyncForm?.();
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
      const removed = arr[idx];
      arr.splice(idx, 1);
      if (removed) onRemove?.(removed);
      refresh();
    });
  });
}

function readCatalogFormFields(p) {
  const idEl = $("#id");
  if (!idEl) return;
  p.id = idEl.value.trim();
  p.folder = $("#folder").value.trim();
  p.displayName = $("#displayName").value.trim();
  p.category = $("#category").value;
  p.description = $("#description").value.trim();
  p.gitUrl = $("#gitUrl").value.trim();
}

function readRecipeFormFields(r) {
  const idEl = $("#id");
  if (!idEl) return;
  const tpl = $("#template");
  const custom = $("#templateCustom");
  r.id = idEl.value.trim();
  r.displayName = $("#displayName").value.trim();
  r.category = $("#category")?.value?.trim() || getFirstCategoryPath(state.recipes?.categories) || "Other";
  r.description = $("#description").value.trim();
  r.template = tpl?.value === "__custom" ? custom.value.trim() : tpl?.value;
  r.outputFile = $("#outputFile").value.trim();
  r.doc = $("#doc").value;
  r.needsHeroType = $("#needsHeroType").checked;
  if (state.recipes) state.recipes.outputRoot = $("#outputRoot").value.trim();
}

function bindCatalogForm(p) {
  bindChipField(
    "dependsOn",
    p.dependsOn || (p.dependsOn = []),
    () => markDirty("catalog"),
    () => readCatalogFormFields(p)
  );

  $("#applyBtn").addEventListener("click", () => {
    const oldId = p.id;
    p.id = $("#id").value.trim();
    p.folder = $("#folder").value.trim();
    p.displayName = $("#displayName").value.trim();
    p.category = $("#category").value;
    p.description = $("#description").value.trim();
    p.gitUrl = $("#gitUrl").value.trim();
    if (!p.gitUrl) delete p.gitUrl;
    if (!p.dependsOn?.length) delete p.dependsOn;
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

  bindChipField(
    "requires",
    r.requires || (r.requires = []),
    () => markDirty("recipes"),
    () => readRecipeFormFields(r)
  );
  bindChipField(
    "dependsOnRecipes",
    r.dependsOnRecipes || (r.dependsOnRecipes = []),
    () => markDirty("recipes"),
    () => readRecipeFormFields(r)
  );

  $("#applyBtn").addEventListener("click", () => {
    const oldId = r.id;
    r.id = $("#id").value.trim();
    r.displayName = $("#displayName").value.trim();
    r.category = $("#category").value.trim() || "Other";
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
    p.category = $("#category")?.value?.trim() || getFirstCategoryPath(state.structure?.categories) || "Other";
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
  const btnCategories = $("#btnCategories");
  if (btnCategories) {
    btnCategories.classList.toggle("hidden", !TREE_TABS.has(state.tab));
    btnCategories.textContent = "+ 分类";
  }
  renderList();
  renderIssues();
  updateDirtyUi();
  renderEditor();
}

async function saveAll() {
  if (!state.online) return;
  const issues = validateAll(state).filter((i) => i.level === "error");
  if (issues.length && !confirm(`存在 ${issues.length} 个错误，仍要保存吗？`)) return;

  try {
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
    if (state.dirty.templatesMeta) {
      syncTemplatesMetaItems();
      await api("/api/data/templatesMeta", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(state.templatesMeta),
      });
    }

    for (const name of [...state.deletedTemplates]) {
      await api(`/api/templates/${encodeURIComponent(name)}`, { method: "DELETE" });
    }

    for (const name of Object.keys(state.dirty.templates)) {
      if (!state.dirty.templates[name]) continue;
      if (state.deletedTemplates.has(name)) continue;
      if (!state.templates.some((t) => t.name === name)) continue;
      const content = state.templateContents[name] ?? "";
      await api(`/api/templates/${encodeURIComponent(name)}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ content }),
      });
    }

    state.dirty = { catalog: false, recipes: false, structure: false, templatesMeta: false, templates: {} };
    state.deletedTemplates.clear();
    state.originalTemplates = new Set(state.templates.map((t) => t.name));
    await refreshGitStatus();
    render();

    const prefix = state.meta?.gitDataPrefix || "Data/";
    const gitTop = state.meta?.gitTopLevel || "<Git 仓库根>";
    const lines = [
      "已保存。",
      "",
      `cd ${gitTop}`,
      `git add ${prefix}`,
      'git commit -m "hub: 描述本次扩展"',
      "git push origin main",
    ];
    alert(lines.join("\n"));
  } catch (err) {
    const msg = String(err.message || err);
    const hint = msg.includes("templatesMeta")
      ? "\n\n提示：请关闭旧 Hub Studio 窗口，在 Tools/HubStudio 目录重新运行 python server.py 后再保存。"
      : "";
    alert(`保存失败：${msg}${hint}`);
  }
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
$("#btnCategories")?.addEventListener("click", () => promptNewCategory());
$("#btnReload").addEventListener("click", async () => {
  if (hasUnsavedChanges() && !confirm("有未保存的修改，重新加载将丢失所有改动。继续？")) return;
  await probeServer();
  await loadAll();
});
$("#btnSaveAll").addEventListener("click", saveAll);

(async function init() {
  await probeServer();
  if (state.online) await loadAll();
  else render();
})();
