import { flattenCategoryPaths, normalizeCategoryTree } from "./categoryTree.js";

function categoryPathSet(categories) {
  return new Set(flattenCategoryPaths(normalizeCategoryTree(categories)));
}

export function validateAll(state) {
  const issues = [];
  issues.push(...validateCatalog(state.catalog));
  issues.push(...validateRecipes(state.recipes, state.catalog));
  issues.push(...validateStructure(state.structure));
  issues.push(...validateTemplates(state.recipes, state.templates));
  return issues;
}

function push(issues, level, scope, message, ref) {
  issues.push({ level, scope, message, ref });
}

export function validateCatalog(catalog) {
  const issues = [];
  if (!catalog) {
    push(issues, "error", "catalog", "catalog 数据为空");
    return issues;
  }

  if (!catalog.frameworkRoot?.trim()) {
    push(issues, "warning", "catalog", "frameworkRoot 为空，将使用默认 Assets/Framework");
  }

  const packages = catalog.packages || [];
  const allIds = new Set();
  for (const p of packages) {
    if (p?.id?.trim()) allIds.add(p.id.trim());
  }

  const categorySet = categoryPathSet(catalog?.categories);
  if (!categorySet.size) {
    push(issues, "warning", "catalog", "categories 为空，侧栏将无法分组");
  }

  const seen = new Set();
  for (let i = 0; i < packages.length; i++) {
    const p = packages[i];
    const label = p.displayName || p.id || `#${i + 1}`;

    if (!p.id?.trim()) push(issues, "error", "catalog", `${label}: 缺少 id`, p.id);
    else if (seen.has(p.id)) push(issues, "error", "catalog", `重复的包 id: ${p.id}`, p.id);
    else seen.add(p.id);

    if (!p.folder?.trim()) push(issues, "error", "catalog", `${label}: 缺少 folder`, p.id);
    if (!p.displayName?.trim()) push(issues, "warning", "catalog", `${p.id || label}: 缺少 displayName`, p.id);
    if (!p.category?.trim()) push(issues, "warning", "catalog", `${p.id || label}: 缺少 category`, p.id);
    else if (categorySet.size && !categorySet.has(p.category.trim())) {
      push(issues, "warning", "catalog", `${p.id}: 分类「${p.category}」不在 categories 树中`, p.id);
    }
    if (p.id === "com.techcosmos.hub") {
      push(issues, "warning", "catalog", "Hub 自身不应出现在 catalog（会被 UI 过滤）", p.id);
    }

    for (const dep of p.dependsOn || []) {
      if (!dep?.trim()) push(issues, "error", "catalog", `${p.id || label}: dependsOn 含空项`, p.id);
      else if (dep === p.id) push(issues, "error", "catalog", `${p.id}: 不能依赖自身`, p.id);
      else if (!allIds.has(dep)) {
        push(issues, "warning", "catalog", `${p.id}: dependsOn 未在 catalog 中找到 ${dep}`, p.id);
      }
    }
  }

  return issues;
}

export function validateRecipes(recipes, catalog) {
  const issues = [];
  if (!recipes) {
    push(issues, "error", "recipes", "recipes 数据为空");
    return issues;
  }

  if (!recipes.outputRoot?.trim()) {
    push(issues, "warning", "recipes", "outputRoot 为空");
  }

  const categorySet = categoryPathSet(recipes.categories);
  if (!categorySet.size) {
    push(issues, "warning", "recipes", "categories 为空，Hub 侧栏将无法分组");
  }

  const packageIds = new Set((catalog?.packages || []).map((p) => p.id).filter(Boolean));
  const list = recipes.recipes || [];
  const ids = new Set();

  for (let i = 0; i < list.length; i++) {
    const r = list[i];
    const label = r.displayName || r.id || `#${i + 1}`;

    if (!r.id?.trim()) push(issues, "error", "recipes", `${label}: 缺少 id`, r.id);
    else if (ids.has(r.id)) push(issues, "error", "recipes", `重复的 recipe id: ${r.id}`, r.id);
    else ids.add(r.id);

    if (!r.template?.trim()) push(issues, "error", "recipes", `${r.id || label}: 缺少 template`, r.id);
    if (!r.displayName?.trim()) push(issues, "warning", "recipes", `${r.id || label}: 缺少 displayName`, r.id);
    if (r.category?.trim() && categorySet.size && !categorySet.has(r.category.trim())) {
      push(issues, "warning", "recipes", `${r.id}: 分类「${r.category}」不在 categories 树中`, r.id);
    }
    const resolved = r.outputFile?.trim() || r.template?.trim();
    if (resolved && /[\\:*?"<>|]/.test(resolved)) {
      push(issues, "warning", "recipes", `${r.id}: 输出路径含非法字符`, r.id);
    }

    for (const req of r.requires || []) {
      if (!req?.trim()) push(issues, "error", "recipes", `${r.id}: requires 含空项`, r.id);
      else if (packageIds.size && !packageIds.has(req)) {
        push(issues, "warning", "recipes", `${r.id}: requires 未在 catalog 中找到 ${req}`, r.id);
      }
    }

    for (const dep of r.dependsOnRecipes || []) {
      if (!dep?.trim()) push(issues, "error", "recipes", `${r.id}: dependsOnRecipes 含空项`, r.id);
      else if (dep === r.id) push(issues, "error", "recipes", `${r.id}: 不能依赖自身`, r.id);
      else if (!ids.has(dep) && !list.some((x) => x.id === dep)) {
        push(issues, "warning", "recipes", `${r.id}: 依赖的 recipe 尚未定义: ${dep}`, r.id);
      }
    }
  }

  return issues;
}

export function validateStructure(structure) {
  const issues = [];
  if (!structure) {
    push(issues, "error", "structure", "structure 数据为空");
    return issues;
  }

  const categorySet = categoryPathSet(structure.categories);
  const presets = structure.presets || [];
  const ids = new Set();

  for (let i = 0; i < presets.length; i++) {
    const p = presets[i];
    const label = p.displayName || p.id || `#${i + 1}`;

    if (!p.id?.trim()) push(issues, "error", "structure", `${label}: 缺少 id`, p.id);
    else if (ids.has(p.id)) push(issues, "error", "structure", `重复的 preset id: ${p.id}`, p.id);
    else ids.add(p.id);

    if (!p.category?.trim()) push(issues, "warning", "structure", `${p.id || label}: 缺少 category`, p.id);
    else if (categorySet.size && !categorySet.has(p.category.trim())) {
      push(issues, "warning", "structure", `${p.id}: 分类「${p.category}」不在 categories 树中`, p.id);
    }

    if (!p.root?.trim()) push(issues, "warning", "structure", `${p.id || label}: root 为空`, p.id);
    else if (!p.root.startsWith("Assets/")) {
      push(issues, "warning", "structure", `${p.id}: root 应以 Assets/ 开头`, p.id);
    }

    for (const folder of p.folders || []) {
      if (!folder?.trim()) push(issues, "error", "structure", `${p.id}: folders 含空项`, p.id);
    }

    for (const extra of p.extraRoots || []) {
      if (!extra.root?.trim()) push(issues, "error", "structure", `${p.id}: extraRoots 缺少 root`, p.id);
      else if (!extra.root.startsWith("Assets/")) {
        push(issues, "warning", "structure", `${p.id}: extraRoot ${extra.root} 应以 Assets/ 开头`, p.id);
      }
    }
  }

  return issues;
}

export function validateTemplates(recipes, templates) {
  const issues = [];
  const names = new Set((templates || []).map((t) => t.name));
  for (const r of recipes?.recipes || []) {
    if (r.template && !names.has(r.template)) {
      push(issues, "warning", "templates", `Recipe ${r.id}: 模板 ${r.template}.txt 不存在`, r.id);
    }
  }
  return issues;
}
