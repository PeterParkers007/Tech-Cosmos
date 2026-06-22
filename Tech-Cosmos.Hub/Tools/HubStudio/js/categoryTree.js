export const PATH_SEP = "/";

export function normalizeNode(node) {
  if (typeof node === "string") return { name: node.trim() || "Other", children: [] };
  return {
    name: (node?.name || "Other").trim() || "Other",
    children: (node?.children || []).map(normalizeNode),
  };
}

export function normalizeCategoryTree(categories, defaults = ["Other"]) {
  if (!Array.isArray(categories) || categories.length === 0) {
    return defaults.map((name) => ({ name, children: [] }));
  }
  return categories.map(normalizeNode);
}

export function flattenCategoryPaths(tree, prefix = "") {
  const nodes = prefix ? tree : normalizeCategoryTree(tree);
  const paths = [];
  for (const node of nodes) {
    const path = prefix ? `${prefix}${PATH_SEP}${node.name}` : node.name;
    paths.push(path);
    if (node.children?.length) paths.push(...flattenCategoryPaths(node.children, path));
  }
  return paths;
}

export function getFirstCategoryPath(tree) {
  const norm = normalizeCategoryTree(tree);
  return norm[0]?.name || "Other";
}

export function findNodeParent(tree, path) {
  const parts = path.split(PATH_SEP);
  if (parts.length <= 1) return { parent: tree, index: tree.findIndex((n) => n.name === parts[0]) };
  let parent = tree;
  for (let i = 0; i < parts.length - 1; i++) {
    const node = parent.find((n) => n.name === parts[i]);
    if (!node) return null;
    if (i === parts.length - 2) {
      return { parent: node.children, index: node.children.findIndex((n) => n.name === parts[parts.length - 1]) };
    }
    parent = node.children;
  }
  return null;
}

export function findNodeByPath(tree, path) {
  const parts = path.split(PATH_SEP);
  let current = tree;
  let node = null;
  for (const part of parts) {
    node = current.find((n) => n.name === part);
    if (!node) return null;
    current = node.children;
  }
  return node;
}

export function categoryPathExists(tree, path) {
  return !!findNodeByPath(tree, path);
}

export function addCategoryNode(tree, parentPath, name) {
  const trimmed = name?.trim();
  if (!trimmed) return false;
  const child = { name: trimmed, children: [] };

  if (!parentPath) {
    if (tree.some((n) => n.name === trimmed)) return false;
    tree.push(child);
    return true;
  }

  const parent = findNodeByPath(tree, parentPath);
  if (!parent) return false;
  if (!parent.children) parent.children = [];
  if (parent.children.some((n) => n.name === trimmed)) return false;
  parent.children.push(child);
  return true;
}

export function renameCategoryAtPath(tree, path, newName) {
  const trimmed = newName?.trim();
  if (!trimmed || trimmed === pathBasename(path)) return path;

  const parentInfo = findNodeParent(tree, path);
  if (!parentInfo || parentInfo.index < 0) return null;

  const sibling = parentInfo.parent.find((n, i) => i !== parentInfo.index && n.name === trimmed);
  if (sibling) return null;

  const oldBasename = pathBasename(path);
  const parentPath = pathParent(path);
  const newPath = parentPath ? `${parentPath}${PATH_SEP}${trimmed}` : trimmed;

  parentInfo.parent[parentInfo.index].name = trimmed;
  return { oldPath: path, newPath, oldBasename, newBasename: trimmed };
}

export function deleteCategoryAtPath(tree, path, protectedNames = ["Other"]) {
  if (protectedNames.includes(pathBasename(path)) || protectedNames.includes(path)) return false;
  const parentInfo = findNodeParent(tree, path);
  if (!parentInfo || parentInfo.index < 0) return false;
  parentInfo.parent.splice(parentInfo.index, 1);
  return true;
}

export function moveCategoryInTree(tree, path, delta) {
  const parentInfo = findNodeParent(tree, path);
  if (!parentInfo || parentInfo.index < 0) return false;
  const next = parentInfo.index + delta;
  if (next < 0 || next >= parentInfo.parent.length) return false;
  const siblings = parentInfo.parent;
  [siblings[parentInfo.index], siblings[next]] = [siblings[next], siblings[parentInfo.index]];
  return true;
}

export function pathBasename(path) {
  const parts = (path || "").split(PATH_SEP);
  return parts[parts.length - 1] || "Other";
}

export function pathParent(path) {
  const parts = (path || "").split(PATH_SEP);
  if (parts.length <= 1) return "";
  return parts.slice(0, -1).join(PATH_SEP);
}

export function remapItemCategoryPath(oldPath, newPath, items, getCategory, setCategory) {
  for (const item of items) {
    const cat = (getCategory(item) || "").trim() || "Other";
    if (cat === oldPath) setCategory(item, newPath);
    else if (cat.startsWith(`${oldPath}${PATH_SEP}`)) {
      setCategory(item, `${newPath}${cat.slice(oldPath.length)}`);
    }
  }
}

export function moveItemsToCategory(items, fromPath, toPath, getCategory, setCategory, fallback = "Other") {
  const target = toPath?.trim() || fallback;
  for (const item of items) {
    const cat = (getCategory(item) || "").trim() || fallback;
    if (cat === fromPath || cat.startsWith(`${fromPath}${PATH_SEP}`)) {
      setCategory(item, target);
    }
  }
}

export function moveItemInCategory(items, itemId, delta, getId, getCategory) {
  const item = items.find((x) => getId(x) === itemId);
  if (!item) return;
  const cat = (getCategory(item) || "").trim() || "Other";
  const inCategory = items.filter((x) => ((getCategory(x) || "").trim() || "Other") === cat);
  const idxInCat = inCategory.findIndex((x) => getId(x) === itemId);
  const next = idxInCat + delta;
  if (idxInCat < 0 || next < 0 || next >= inCategory.length) return;
  const globalIdx = items.findIndex((x) => getId(x) === itemId);
  const swapId = getId(inCategory[next]);
  const swapGlobalIdx = items.findIndex((x) => getId(x) === swapId);
  [items[globalIdx], items[swapGlobalIdx]] = [items[swapGlobalIdx], items[globalIdx]];
}

export function getCategoryOptions(tree, current) {
  const set = new Set(flattenCategoryPaths(normalizeCategoryTree(tree)));
  if (current?.trim()) set.add(current.trim());
  return [...set].filter(Boolean).sort((a, b) => a.localeCompare(b));
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function escapeAttr(s) {
  return escapeHtml(s);
}

function bindDropTarget(el, categoryPath, dragMime, onDrop) {
  el.addEventListener("dragover", (e) => {
    e.preventDefault();
    e.stopPropagation();
    e.dataTransfer.dropEffect = "move";
    el.classList.add("drag-over");
  });
  el.addEventListener("dragleave", (e) => {
    e.stopPropagation();
    if (el.contains(e.relatedTarget)) return;
    el.classList.remove("drag-over");
  });
  el.addEventListener("drop", (e) => {
    e.preventDefault();
    e.stopPropagation();
    el.classList.remove("drag-over");
    const id = e.dataTransfer.getData(dragMime);
    if (id) onDrop(id, categoryPath);
  });
}

function nodeHasVisibleContent(node, path, depth, ctx) {
  const items = ctx.itemsInPath(path);
  if (items.length) return true;
  for (const child of node.children || []) {
    const childPath = path ? `${path}${PATH_SEP}${child.name}` : child.name;
    if (nodeHasVisibleContent(child, childPath, depth + 1, ctx)) return true;
  }
  return false;
}

function renderNodeBlock(node, path, depth, ctx) {
  const items = ctx.itemsInPath(path);
  const q = ctx.search.trim().toLowerCase();
  if (q && !nodeHasVisibleContent(node, path, depth, ctx)) return "";

  const itemRows = items
    .map((item) => {
      const id = ctx.getItemId(item);
      const selected = id === ctx.selectedId;
      const moveButtons = ctx.showMoveButtons
        ? `
            <button type="button" class="btn btn-sm btn-ghost" data-move="up" title="上移">↑</button>
            <button type="button" class="btn btn-sm btn-ghost" data-move="down" title="下移">↓</button>`
        : "";
      return `
        <li class="category-tree-item ${selected ? "selected" : ""}" draggable="true" data-item-id="${escapeAttr(id)}">
          <span class="category-drag-handle" title="拖到其他分类">⠿</span>
          <div class="item-main">
            <div class="item-title">${escapeHtml(ctx.getItemTitle(item))}</div>
            <div class="item-sub">${escapeHtml(ctx.getItemSub(item) || "")}</div>
          </div>
          <div class="item-actions">${moveButtons}
            <button type="button" class="btn btn-sm btn-danger" data-delete title="删除">×</button>
          </div>
        </li>`;
    })
    .join("");

  const childBlocks = (node.children || [])
    .map((child) => {
      const childPath = `${path}${PATH_SEP}${child.name}`;
      return renderNodeBlock(child, childPath, depth + 1, ctx);
    })
    .filter(Boolean)
    .join("");

  const hasChildren = Boolean(childBlocks);
  const emptyHint =
    !itemRows && !hasChildren && !q
      ? `<li class="category-tree-empty">拖入条目，或点 + 子分类 / + 新建</li>`
      : "";

  const itemsSection =
    itemRows || emptyHint
      ? `<ul class="category-drop${hasChildren ? " category-drop--compact" : ""}">${itemRows}${emptyHint}</ul>`
      : "";

  const childSection = hasChildren
    ? `<div class="category-children"><ul class="category-tree category-tree--nested">${childBlocks}</ul></div>`
    : "";

  return `
    <li class="category-block" data-category-path="${escapeAttr(path)}" data-depth="${depth}">
      <div class="category-header" style="--tree-depth:${depth}">
        <span class="category-name">${escapeHtml(node.name)}</span>
        <span class="category-count">${items.length}</span>
        <div class="category-actions">
          <button type="button" class="btn btn-sm btn-ghost" data-move-category="up" title="分类上移">↑</button>
          <button type="button" class="btn btn-sm btn-ghost" data-move-category="down" title="分类下移">↓</button>
          <button type="button" class="btn btn-sm btn-ghost" data-add-sub-category title="新建子分类">+</button>
          <button type="button" class="btn btn-sm btn-ghost" data-rename-category title="重命名">✎</button>
          <button type="button" class="btn btn-sm btn-danger" data-delete-category title="删除分类">×</button>
        </div>
      </div>
      ${itemsSection}${childSection}
    </li>`;
}

export function renderCategoryTreeList(container, ctx) {
  container.classList.add("category-tree");
  const tree = normalizeCategoryTree(ctx.tree);

  const itemsInPath = (path) =>
    ctx.items.filter((item) => {
      const cat = (ctx.getItemCategory(item) || "").trim() || "Other";
      if (cat !== path) return false;
      if (!ctx.search.trim()) return true;
      const hay = `${ctx.getItemTitle(item)} ${ctx.getItemId(item)} ${ctx.getItemSub(item) || ""} ${path}`.toLowerCase();
      return hay.includes(ctx.search.trim().toLowerCase());
    });

  const renderCtx = { ...ctx, itemsInPath };
  const blocks = tree.map((node) => renderNodeBlock(node, node.name, 0, renderCtx)).join("");

  container.innerHTML =
    blocks +
    `<li class="category-tree-add">
      <button type="button" class="btn btn-sm btn-accent" id="btnAddCategoryInline">+ 新建分类</button>
    </li>`;

  container.querySelectorAll(".category-block").forEach((block) => {
    const categoryPath = block.getAttribute("data-category-path");
    const header = block.querySelector(":scope > .category-header");
    const drop = block.querySelector(":scope > .category-drop");
    bindDropTarget(header, categoryPath, ctx.dragMime, ctx.onMoveToCategory);
    if (drop) bindDropTarget(drop, categoryPath, ctx.dragMime, ctx.onMoveToCategory);

    block.querySelector("[data-add-sub-category]")?.addEventListener("click", (e) => {
      e.stopPropagation();
      ctx.onAddSubCategory(categoryPath);
    });
    block.querySelector("[data-rename-category]")?.addEventListener("click", (e) => {
      e.stopPropagation();
      ctx.onRenameCategory(categoryPath);
    });
    block.querySelector("[data-delete-category]")?.addEventListener("click", (e) => {
      e.stopPropagation();
      ctx.onDeleteCategory(categoryPath);
    });
    block.querySelector('[data-move-category="up"]')?.addEventListener("click", (e) => {
      e.stopPropagation();
      ctx.onMoveCategory?.(categoryPath, -1);
    });
    block.querySelector('[data-move-category="down"]')?.addEventListener("click", (e) => {
      e.stopPropagation();
      ctx.onMoveCategory?.(categoryPath, 1);
    });
  });

  container.querySelectorAll(".category-tree-item").forEach((li) => {
    const id = li.getAttribute("data-item-id");
    if (!id) return;

    li.addEventListener("dragstart", (e) => {
      if (e.target.closest("button")) {
        e.preventDefault();
        return;
      }
      e.dataTransfer.setData(ctx.dragMime, id);
      e.dataTransfer.effectAllowed = "move";
      li.classList.add("dragging");
    });
    li.addEventListener("dragend", () => li.classList.remove("dragging"));

    li.addEventListener("click", (e) => {
      if (e.target.closest("[data-delete],[data-move],button,.category-drag-handle")) return;
      ctx.onSelect(id);
    });

    li.querySelector("[data-delete]")?.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      ctx.onDelete(id);
    });
    li.querySelector('[data-move="up"]')?.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      ctx.onMoveInCategory(id, -1);
    });
    li.querySelector('[data-move="down"]')?.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      ctx.onMoveInCategory(id, 1);
    });
  });

  container.querySelector("#btnAddCategoryInline")?.addEventListener("click", (e) => {
    e.preventDefault();
    ctx.onAddRootCategory();
  });
}
