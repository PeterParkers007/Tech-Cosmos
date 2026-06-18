#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    public sealed class TechCosmosHubWindow : EditorWindow
    {
        private enum HubTab { Packages, Glue, ProjectStructure }

        private HubTab _tab = HubTab.Packages;
        private PackageCatalogFile _catalog;
        private GlueRecipeFile _recipes;
        private ProjectStructureFile _structure;
        private string _search = string.Empty;
        private string _selectedPackageId;
        private readonly HashSet<string> _selectedPackageIds = new();
        private string _selectedRecipeId;
        private string _selectedStructurePresetId;
        private bool _pendingResolve;

        private VisualElement _root;
        private VisualElement _header;
        private VisualElement _body;
        private VisualElement _sidebarList;
        private VisualElement _detailPanel;
        private Label _statInstalled;
        private Label _statLocal;
        private Label _statTotal;
        private Button _tabPackages;
        private Button _tabGlue;
        private Button _tabProjectStructure;
        private ToolbarSearchField _searchField;
        private Button _batchImportBtn;
        private VisualElement _tabs;
        private StyleSheet _hubStylesheet;

        [MenuItem("Tech-Cosmos/Hub", false, 0)]
        public static void Open()
        {
            var w = GetWindow<TechCosmosHubWindow>("Tech-Cosmos Hub");
            w.minSize = new Vector2(880, 520);
            w.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            if (!HubTheme.TryCloneShell(rootVisualElement, out _root))
            {
                _root = new VisualElement();
                _root.AddToClassList("hub-root");
                _root.style.flexGrow = 1;
                rootVisualElement.Add(_root);
            }
            else
            {
                _root.style.flexGrow = 1;
            }

            BuildHeader();
            BuildTabs();
            BuildBody();

            HubShellLayout.ApplyRoot(_root);
            LoadStylesheet(forceReload: true);
            ApplyVisualTheme();

            _root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            ReloadData();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        internal static void RefreshAllOpenWindows()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<TechCosmosHubWindow>())
            {
                if (w != null)
                    w.RefreshAfterPackageChange();
            }
        }

        private void RefreshAfterPackageChange()
        {
            LoadStylesheet(forceReload: true);
            ReloadData();
        }

        private void LoadStylesheet(bool forceReload = false)
        {
            if (!HubTheme.Attach(rootVisualElement, ref _hubStylesheet, forceReload))
            {
                var paths = string.Join("\n  ", HubTheme.EnumerateStylesheetAssetPaths());
                Debug.LogError(
                    "[Tech-Cosmos Hub] TechCosmosHub.uss 未加载，主题样式缺失（布局已由 HubShellLayout 保底）。\n" +
                    $"尝试路径:\n  {paths}");
                return;
            }

            if (_root != null && _hubStylesheet != null && !_root.styleSheets.Contains(_hubStylesheet))
                _root.styleSheets.Add(_hubStylesheet);
        }

        private void ApplyVisualTheme()
        {
            var sidebar = _body?.Q(className: "hub-sidebar");
            HubColors.ApplyShell(_root, _header, _tabs, _body, sidebar, _detailPanel);
            HubColors.RefreshTab(_tabPackages, _tab == HubTab.Packages);
            HubColors.RefreshTab(_tabGlue, _tab == HubTab.Glue);
            HubColors.RefreshTab(_tabProjectStructure, _tab == HubTab.ProjectStructure);
            if (_searchField != null)
                HubColors.ApplySearchField(_searchField);
            EnsureInteractionFeedback();
        }

        private void OnEditorUpdate()
        {
            if (!_pendingResolve) return;
            _pendingResolve = false;
            Client.Resolve();
            ReloadData();
        }

        private void ReloadData()
        {
            PackageReadmeLoader.ClearCache();

            var catalogPath = HubPaths.CatalogJson;
            if (!File.Exists(catalogPath))
            {
                Debug.LogWarning(
                    $"[Tech-Cosmos Hub] 找不到 catalog 文件（若刚在 Package Manager 点了 Update，请点 Hub 内「刷新」或稍候重试）:\n{catalogPath}");
            }

            _catalog = HubDataLoader.LoadCatalog();
            _recipes = HubDataLoader.LoadRecipes();
            _structure = HubDataLoader.LoadProjectStructure();

            _selectedPackageIds.Remove(HubCatalog.SelfPackageId);

            var browsable = (_catalog?.BrowsablePackages() ?? Enumerable.Empty<PackageCatalogEntry>()).ToList();
            if ((string.IsNullOrEmpty(_selectedPackageId) || _selectedPackageId == HubCatalog.SelfPackageId) &&
                browsable.Count > 0)
                _selectedPackageId = browsable[0].id;

            if (string.IsNullOrEmpty(_selectedRecipeId) && _recipes?.recipes?.Length > 0)
                _selectedRecipeId = _recipes.recipes[0].id;

            _selectedStructurePresetId = HubSettings.SelectedStructurePresetId;
            if (ProjectStructureGenerator.FindPreset(_structure, _selectedStructurePresetId) == null &&
                _structure?.presets?.Length > 0)
                _selectedStructurePresetId = _structure.presets[0].id;

            RefreshStats();
            RefreshSidebar();
            RefreshDetail();
            ApplyVisualTheme();
        }

        private void EnsureInteractionFeedback()
        {
            if (_root == null) return;
            HubUiFactory.ApplyInteractionFeedback(_root);
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (_body == null || _header == null) return;
            var w = evt.newRect.width;
            _body.EnableInClassList("hub-body--narrow", w < 760);
            _header.EnableInClassList("hub-header--compact", w < 920);
            _header.EnableInClassList("hub-header--stacked", w < 640);
            _body.style.flexDirection = w < 760 ? FlexDirection.Column : FlexDirection.Row;
        }

        private sealed class DetailShell
        {
            public VisualElement Top;
            public ScrollView Scroll;
            public VisualElement ScrollContent;
            public VisualElement Bottom;
        }

        private DetailShell CreateDetailShell(bool withBottom = false)
        {
            var shell = new DetailShell();

            shell.Top = new VisualElement();
            shell.Top.AddToClassList("hub-detail-top");
            _detailPanel.Add(shell.Top);

            shell.Scroll = new ScrollView(ScrollViewMode.Vertical);
            shell.Scroll.AddToClassList("hub-detail-scroll");
            shell.ScrollContent = new VisualElement();
            shell.ScrollContent.AddToClassList("hub-detail-scroll-content");
            shell.ScrollContent.style.flexDirection = FlexDirection.Column;
            shell.ScrollContent.style.flexShrink = 0;
            shell.ScrollContent.style.alignItems = Align.Stretch;
            shell.Scroll.contentContainer.style.flexDirection = FlexDirection.Column;
            shell.Scroll.contentContainer.style.alignItems = Align.Stretch;
            shell.Scroll.contentContainer.Add(shell.ScrollContent);
            _detailPanel.Add(shell.Scroll);
            HubShellLayout.ApplyDetailShell(shell.Top, shell.Scroll, shell.ScrollContent);

            if (withBottom)
            {
                shell.Bottom = new VisualElement();
                shell.Bottom.AddToClassList("hub-detail-bottom");
                _detailPanel.Add(shell.Bottom);
                HubShellLayout.ApplyDetailBottom(shell.Bottom);
            }

            return shell;
        }

        private static void AddMarkdownCard(VisualElement scrollContent, string markdown, string baseDirectory = null)
        {
            var card = new VisualElement();
            card.AddToClassList("hub-readme-card");
            card.style.flexShrink = 0;
            card.style.flexDirection = FlexDirection.Column;
            HubColors.ApplyReadmeCard(card);
            card.Add(HubMarkdownRenderer.Render(markdown, baseDirectory));
            scrollContent.Add(card);
        }

        private static void AddReadmeCard(VisualElement scrollContent, PackageCatalogEntry pkg, PackageCatalogFile catalog)
        {
            var markdown = PackageReadmeLoader.LoadPreview(pkg, catalog);
            var readmePath = PackageReadmeLoader.ResolveReadmeFullPath(pkg, catalog);
            var baseDir = string.IsNullOrEmpty(readmePath) ? null : Path.GetDirectoryName(readmePath);
            AddMarkdownCard(scrollContent, markdown, baseDir);
        }

        private void BuildHeader()
        {
            _header = new VisualElement();
            _header.AddToClassList("hub-header");

            var brand = new VisualElement();
            brand.AddToClassList("hub-header-brand");
            brand.Add(HubUiFactory.Label("Tech-Cosmos Hub", "hub-title"));
            brand.Add(HubUiFactory.Label("浏览技术结晶 · 按需导入 · 条件生成胶水", "hub-subtitle"));
            _header.Add(brand);

            var stats = new VisualElement();
            stats.AddToClassList("hub-stats");

            _statInstalled = AddStatChip(stats, "0", "已导入");
            _statLocal = AddStatChip(stats, "0", "本地");
            _statTotal = AddStatChip(stats, "0", "总计");
            _header.Add(stats);

            var actions = new VisualElement();
            actions.AddToClassList("hub-header-actions");
            actions.Add(HubUiFactory.Button("刷新", "hub-btn hub-btn--ghost", ReloadData));
            actions.Add(HubUiFactory.Button("解析 Packages", "hub-btn hub-btn--ghost", () =>
            {
                _pendingResolve = true;
                Client.Resolve();
            }));
            _header.Add(actions);

            HubShellLayout.ApplyHeader(_header);
            HubShellLayout.ApplyStats(stats);
            HubShellLayout.ApplyHeaderActions(actions);
            _root.Add(_header);
        }

        private static Label AddStatChip(VisualElement parent, string value, string label)
        {
            var chip = new VisualElement();
            chip.AddToClassList("hub-stat-chip");
            var val = HubUiFactory.Label(value, "hub-stat-value");
            var cap = HubUiFactory.Label(label, "hub-stat-label");
            chip.Add(val);
            chip.Add(cap);
            HubColors.ApplyStatChip(chip, val, cap);
            parent.Add(chip);
            return val;
        }

        private void BuildTabs()
        {
            _tabs = new VisualElement();
            _tabs.AddToClassList("hub-tabs");

            _tabPackages = HubUiFactory.Button("技术框架", "hub-tab hub-tab--active", () => SetTab(HubTab.Packages));
            _tabGlue = HubUiFactory.Button("胶水生成", "hub-tab", () => SetTab(HubTab.Glue));
            _tabProjectStructure = HubUiFactory.Button("项目结构", "hub-tab", () => SetTab(HubTab.ProjectStructure));

            _tabs.Add(_tabPackages);
            _tabs.Add(_tabGlue);
            _tabs.Add(_tabProjectStructure);
            HubShellLayout.ApplyTabs(_tabs);
            HubColors.RefreshTab(_tabPackages, _tab == HubTab.Packages);
            HubColors.RefreshTab(_tabGlue, _tab == HubTab.Glue);
            HubColors.RefreshTab(_tabProjectStructure, _tab == HubTab.ProjectStructure);
            _root.Add(_tabs);
        }

        private void SetTab(HubTab tab)
        {
            _tab = tab;
            _tabPackages.EnableInClassList("hub-tab--active", tab == HubTab.Packages);
            _tabGlue.EnableInClassList("hub-tab--active", tab == HubTab.Glue);
            _tabProjectStructure.EnableInClassList("hub-tab--active", tab == HubTab.ProjectStructure);
            HubColors.RefreshTab(_tabPackages, tab == HubTab.Packages);
            HubColors.RefreshTab(_tabGlue, tab == HubTab.Glue);
            HubColors.RefreshTab(_tabProjectStructure, tab == HubTab.ProjectStructure);

            if (_searchField != null)
                _searchField.style.display = tab == HubTab.Packages ? DisplayStyle.Flex : DisplayStyle.None;

            RefreshSidebar();
            RefreshDetail();
        }

        private void BuildBody()
        {
            _body = new VisualElement();
            _body.AddToClassList("hub-body");

            var sidebar = new VisualElement();
            sidebar.AddToClassList("hub-sidebar");

            var searchWrap = new VisualElement();
            searchWrap.AddToClassList("hub-search-wrap");

            _searchField = new ToolbarSearchField { value = _search };
            _searchField.AddToClassList("hub-sidebar-search");
            _searchField.style.flexGrow = 1;
            _searchField.style.flexShrink = 1;
            _searchField.style.minWidth = 0;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? string.Empty;
                RefreshSidebar();
            });
            searchWrap.Add(_searchField);
            sidebar.Add(searchWrap);
            HubShellLayout.ApplySidebarSearchWrap(searchWrap);

            var sidebarScroll = new ScrollView(ScrollViewMode.Vertical);
            sidebarScroll.AddToClassList("hub-sidebar-scroll");
            _sidebarList = new VisualElement();
            sidebarScroll.Add(_sidebarList);
            sidebar.Add(sidebarScroll);
            HubShellLayout.ApplySidebarScroll(sidebarScroll);

            var sidebarFooter = new VisualElement();
            sidebarFooter.AddToClassList("hub-sidebar-footer");
            sidebar.Add(sidebarFooter);
            HubShellLayout.ApplySidebarFooter(sidebarFooter);
            _sidebarFooter = sidebarFooter;

            _detailPanel = new VisualElement();
            _detailPanel.AddToClassList("hub-detail");

            _body.Add(sidebar);
            _body.Add(_detailPanel);

            HubShellLayout.ApplyBody(_body);
            HubShellLayout.ApplySidebar(sidebar);
            HubShellLayout.ApplyDetailPanel(_detailPanel);
            _root.Add(_body);
        }

        private VisualElement _sidebarFooter;

        private void RefreshStats()
        {
            if (_catalog == null) return;

            int installed = 0, local = 0, total = 0;
            foreach (var p in _catalog.BrowsablePackages())
            {
                total++;
                var presence = PackageDetector.GetPresence(p, _catalog);
                if (presence is PackagePresence.InManifest or PackagePresence.AssetsEmbedded)
                    installed++;
                else if (presence == PackagePresence.LocalOnly)
                    local++;
            }

            _statInstalled.text = installed.ToString();
            _statLocal.text = local.ToString();
            _statTotal.text = total.ToString();
        }

        private void RefreshSidebar()
        {
            _sidebarList.Clear();
            _sidebarFooter.Clear();

            if (_tab == HubTab.Packages)
                BuildPackageSidebar();
            else if (_tab == HubTab.Glue)
                BuildGlueSidebar();
            else
                BuildProjectStructureSidebar();

            EnsureInteractionFeedback();
        }

        private void BuildPackageSidebar()
        {
            var browsable = _catalog?.BrowsablePackages().ToList();
            if (browsable == null || browsable.Count == 0)
            {
                _sidebarList.Add(HubUiFactory.Label("未找到 package-catalog.json", "hub-empty-text"));
                return;
            }

            var filtered = browsable
                .Where(MatchesSearch)
                .GroupBy(p => p.category ?? "Other")
                .OrderBy(g => g.Key);

            foreach (var group in filtered)
            {
                _sidebarList.Add(HubUiFactory.Label(group.Key.ToUpperInvariant(), "hub-category-label"));
                if (_sidebarList[_sidebarList.childCount - 1] is Label catLabel)
                    HubColors.ApplyCategoryLabel(catLabel);

                foreach (var pkg in group.OrderBy(p => p.displayName))
                {
                    var presence = PackageDetector.GetPresence(pkg, _catalog);
                    var item = new VisualElement();
                    item.AddToClassList("hub-list-item");
                    HubShellLayout.ApplyListItem(item);
                    HubColors.RefreshListItem(item, pkg.id == _selectedPackageId);
                    if (pkg.id == _selectedPackageId)
                        item.AddToClassList("hub-list-item--selected");
                    if (_selectedPackageIds.Contains(pkg.id))
                        item.AddToClassList("hub-list-item--checked");

                    var check = new Toggle { value = _selectedPackageIds.Contains(pkg.id) };
                    check.AddToClassList("hub-list-check");
                    HubColors.RefreshToggle(check);
                    var capturedPkg = pkg;
                    check.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue) _selectedPackageIds.Add(capturedPkg.id);
                        else _selectedPackageIds.Remove(capturedPkg.id);
                        UpdateBatchImportButton();
                        RefreshSidebar();
                    });
                    check.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
                    item.Add(check);

                    item.Add(HubUiFactory.StatusDot(presence));

                    var depPlan = PackageDependencyResolver.Evaluate(pkg, _catalog);
                    if (!depPlan.CanImport && !PackageDetector.IsRequirementMet(pkg.id, _catalog)
                        && depPlan.DependsOn.Count > 0)
                    {
                        item.Add(HubUiFactory.Label("!", "hub-list-item-dep-warn"));
                        item.tooltip = depPlan.BlockReason;
                    }

                    var nameLbl = HubUiFactory.Label(pkg.displayName, "hub-list-item-name");
                    HubColors.ApplyListItemName(nameLbl, pkg.id == _selectedPackageId);
                    item.Add(nameLbl);

                    item.RegisterCallback<ClickEvent>(evt =>
                    {
                        if (evt.target is Toggle) return;
                        _selectedPackageId = capturedPkg.id;
                        RefreshSidebar();
                        RefreshDetail();
                    });

                    _sidebarList.Add(item);
                }
            }

            var visible = browsable.Count(MatchesSearch);
            _sidebarFooter.Add(HubUiFactory.ImportModeSelector(() =>
            {
                RefreshSidebar();
                RefreshDetail();
            }));

            var batchRow = new VisualElement();
            batchRow.AddToClassList("hub-batch-row");
            HubShellLayout.ApplyBatchRow(batchRow);
            _batchImportBtn = HubUiFactory.Button("导入选中 (0)", "hub-btn hub-btn--accent", ImportSelectedPackages);
            batchRow.Add(_batchImportBtn);
            batchRow.Add(HubUiFactory.Button("全选可见", "hub-btn hub-btn--ghost", SelectAllVisiblePackages));
            batchRow.Add(HubUiFactory.Button("清除", "hub-btn hub-btn--ghost", () =>
            {
                _selectedPackageIds.Clear();
                UpdateBatchImportButton();
                RefreshSidebar();
            }));
            HubShellLayout.ApplyBtnRow(batchRow);
            _sidebarFooter.Add(batchRow);
            UpdateBatchImportButton();

            _sidebarFooter.Add(HubUiFactory.Label($"{visible} / {browsable.Count} 项", "hub-sidebar-hint"));
        }

        private void SelectAllVisiblePackages()
        {
            foreach (var pkg in _catalog.BrowsablePackages().Where(MatchesSearch))
                _selectedPackageIds.Add(pkg.id);
            UpdateBatchImportButton();
            RefreshSidebar();
        }

        private void UpdateBatchImportButton()
        {
            if (_batchImportBtn == null) return;
            var label = HubSettings.ImportMode == HubImportMode.AssetsEmbed ? "嵌入选中" : "导入选中";
            _batchImportBtn.text = $"{label} ({_selectedPackageIds.Count})";
            _batchImportBtn.SetEnabled(_selectedPackageIds.Count > 0);
            HubColors.RefreshButton(_batchImportBtn, HubUiFactory.JoinClasses(_batchImportBtn));
        }

        private void ImportSelectedPackages()
        {
            if (_catalog?.packages == null || _selectedPackageIds.Count == 0) return;

            var entries = _catalog.BrowsablePackages()
                .Where(p => _selectedPackageIds.Contains(p.id))
                .ToList();
            var result = PackageInstaller.ImportPackages(entries, _catalog);

            if (HubSettings.ImportMode == HubImportMode.GitUpm && result.Succeeded.Count > 0)
                _pendingResolve = true;

            var msg = $"成功: {result.Succeeded.Count}";
            if (result.Skipped.Count > 0) msg += $"\n跳过: {result.Skipped.Count}";
            if (result.Failed.Count > 0) msg += $"\n失败:\n• " + string.Join("\n• ", result.Failed);

            EditorUtility.DisplayDialog(
                result.Failed.Count > 0 ? "批量导入完成（部分失败）" : "批量导入完成",
                msg, "确定");

            if (result.Succeeded.Count > 0)
                EditorApplication.delayCall += ReloadData;
        }

        private void BuildGlueSidebar()
        {
            if (_recipes?.recipes == null || _recipes.recipes.Length == 0)
            {
                _sidebarList.Add(HubUiFactory.Label("未找到 glue-recipes.json", "hub-empty-text"));
                return;
            }

            _sidebarList.Add(HubUiFactory.Label("GLUE RECIPES", "hub-category-label"));

            foreach (var recipe in _recipes.recipes)
            {
                var status = GlueGenerator.Evaluate(recipe, _catalog, _recipes);
                var selected = recipe.id == _selectedRecipeId;
                var captured = recipe;

                var item = HubUiFactory.SidebarItem(selected, el =>
                {
                    var dot = new VisualElement();
                    dot.AddToClassList("hub-status-dot");
                    HubColors.ApplyStatusDot(dot, status.CanGenerate);
                    el.Add(dot);

                    var nameLbl = HubUiFactory.Label(recipe.displayName, "hub-list-item-name");
                    HubColors.ApplyListItemName(nameLbl, selected);
                    el.Add(nameLbl);
                });

                item.RegisterCallback<ClickEvent>(_ =>
                {
                    _selectedRecipeId = captured.id;
                    RefreshSidebar();
                    RefreshDetail();
                });

                _sidebarList.Add(item);
            }

            var batchRow = new VisualElement();
            batchRow.AddToClassList("hub-batch-row");
            HubShellLayout.ApplyBatchRow(batchRow);
            batchRow.Add(HubUiFactory.Button("生成全部就绪项", "hub-btn hub-btn--accent", () =>
            {
                GlueGenerator.GenerateAllReady(_catalog, _recipes);
                RefreshSidebar();
                RefreshDetail();
            }));
            _sidebarFooter.Add(batchRow);
        }

        private bool MatchesSearch(PackageCatalogEntry p)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            var q = _search.Trim();
            return ContainsIgnoreCase(p.displayName, q)
                   || ContainsIgnoreCase(p.description, q)
                   || ContainsIgnoreCase(p.id, q)
                   || ContainsIgnoreCase(p.category, q);
        }

        private static bool ContainsIgnoreCase(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return false;
            return haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RefreshDetail()
        {
            _detailPanel.Clear();

            if (_tab == HubTab.Packages)
                BuildPackageDetail();
            else if (_tab == HubTab.Glue)
                BuildGlueDetail();
            else
                BuildProjectStructureDetail();

            EnsureInteractionFeedback();
        }

        private void BuildPackageDetail()
        {
            var pkg = FindPackage(_selectedPackageId);
            if (pkg == null)
            {
                ShowEmpty("← 在左侧选择一个框架", "查看 README、状态和导入操作");
                return;
            }

            var presence = PackageDetector.GetPresence(pkg, _catalog);
            var importPlan = PackageDependencyResolver.Evaluate(pkg, _catalog);
            var isAssetsMode = HubSettings.ImportMode == HubImportMode.AssetsEmbed;
            var shell = CreateDetailShell();

            var header = new VisualElement();
            header.AddToClassList("hub-detail-header");

            var titleBlock = new VisualElement();
            titleBlock.AddToClassList("hub-detail-title-block");
            titleBlock.Add(HubUiFactory.Label(pkg.displayName, "hub-detail-title"));
            titleBlock.Add(HubUiFactory.Label(pkg.description, "hub-detail-desc"));

            var meta = HubUiFactory.MetaRow();
            meta.Add(HubUiFactory.Label(pkg.category, "hub-chip hub-chip--category"));
            meta.Add(HubUiFactory.Label(pkg.id, "hub-chip"));
            meta.Add(PresenceBadge(presence));
            meta.Add(HubUiFactory.Label(
                isAssetsMode ? $"目标: {HubSettings.AssetsPackageRoot}" : "目标: Packages/manifest.json",
                "hub-chip"));
            titleBlock.Add(meta);
            header.Add(titleBlock);
            HubShellLayout.ApplyDetailHeader(header);
            shell.Top.Add(header);

            var btnRow = new VisualElement();
            btnRow.AddToClassList("hub-btn-row");

            var importLabel = isAssetsMode ? "嵌入到 Assets" : "Git 导入";
            if (importPlan.PendingDependencyCount > 0)
                importLabel += $"（+{importPlan.PendingDependencyCount} 依赖）";

            var importBtn = HubUiFactory.Button(importLabel, "hub-btn hub-btn--primary", () => ImportPackage(pkg));
            importBtn.SetEnabled(importPlan.CanImport);
            HubColors.RefreshButton(importBtn, HubUiFactory.JoinClasses(importBtn));
            importBtn.tooltip = importPlan.CanImport
                ? (importPlan.PendingDependencyCount > 0
                    ? "将自动先导入未安装的依赖包"
                    : string.Empty)
                : importPlan.BlockReason;
            btnRow.Add(importBtn);

            var updateLabel = isAssetsMode ? "重新同步" : "更新";
            var updateBtn = HubUiFactory.Button(updateLabel, "hub-btn hub-btn--accent", () => UpdatePackage(pkg));
            updateBtn.SetEnabled(PackageDetector.CanUpdate(pkg, _catalog));
            HubColors.RefreshButton(updateBtn, HubUiFactory.JoinClasses(updateBtn));
            btnRow.Add(updateBtn);

            var removeLabel = isAssetsMode ? "从 Assets 移除" : "从 Manifest 移除";
            var removeBtn = HubUiFactory.Button(removeLabel, "hub-btn hub-btn--danger", () =>
            {
                if (isAssetsMode)
                {
                    if (!EditorUtility.DisplayDialog("移除框架", $"删除 {HubSettings.AssetsPackageRoot}/{pkg.folder}？", "删除", "取消"))
                        return;
                }

                PackageInstaller.RemovePackage(pkg);
                if (!isAssetsMode) _pendingResolve = true;
                EditorApplication.delayCall += ReloadData;
            });
            removeBtn.SetEnabled(isAssetsMode
                ? presence == PackagePresence.AssetsEmbedded
                : presence == PackagePresence.InManifest);
            HubColors.RefreshButton(removeBtn, HubUiFactory.JoinClasses(removeBtn));
            btnRow.Add(removeBtn);

            if (isAssetsMode)
            {
                var locateBtn = HubUiFactory.Button("在 Project 中定位", "hub-btn hub-btn--ghost", () =>
                {
                    var folder = $"{HubSettings.AssetsPackageRoot}/{pkg.folder}";
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(folder);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                });
                btnRow.Add(locateBtn);
            }

            var readmePath = PackageReadmeLoader.GetReadmeAssetPath(pkg, _catalog);
            var readmeFullPath = PackageReadmeLoader.ResolveReadmeFullPath(pkg, _catalog);
            var openBtn = HubUiFactory.Button("打开 README", "hub-btn hub-btn--ghost", () =>
            {
                if (!string.IsNullOrEmpty(readmePath))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(readmePath);
                    if (asset != null)
                    {
                        AssetDatabase.OpenAsset(asset);
                        EditorGUIUtility.PingObject(asset);
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(readmeFullPath) && File.Exists(readmeFullPath))
                    EditorUtility.OpenWithDefaultApp(readmeFullPath);
            });
            openBtn.SetEnabled(!string.IsNullOrEmpty(readmePath)
                || (!string.IsNullOrEmpty(readmeFullPath) && File.Exists(readmeFullPath)));
            HubColors.RefreshButton(openBtn, HubUiFactory.JoinClasses(openBtn));
            btnRow.Add(openBtn);

            HubShellLayout.ApplyBtnRow(btnRow);
            shell.Top.Add(btnRow);

            shell.ScrollContent.Add(HubUiFactory.Label("依赖关系", "hub-section-title"));
            shell.ScrollContent.Add(HubUiFactory.DependencySection(importPlan, id =>
            {
                if (string.IsNullOrEmpty(id)) return;
                _selectedPackageId = id;
                RefreshSidebar();
                RefreshDetail();
            }));

            shell.ScrollContent.Add(HubUiFactory.Label("介绍 / README", "hub-section-title"));
            AddReadmeCard(shell.ScrollContent, pkg, _catalog);
        }

        private void BuildGlueDetail()
        {
            var recipe = FindRecipe(_selectedRecipeId);
            if (recipe == null)
            {
                ShowEmpty("← 选择一条胶水 Recipe", "查看生成条件与详细说明");
                return;
            }

            var status = GlueGenerator.Evaluate(recipe, _catalog, _recipes);
            var shell = CreateDetailShell();

            shell.Top.Add(HubUiFactory.Label(recipe.displayName, "hub-detail-title"));
            shell.Top.Add(HubUiFactory.Label(recipe.description, "hub-detail-desc"));

            var header = new VisualElement();
            header.AddToClassList("hub-detail-header");
            HubShellLayout.ApplyDetailHeader(header);
            var meta = HubUiFactory.MetaRow();
            meta.Add(HubUiFactory.Label(
                $"输出 → {_recipes.outputRoot}/{GlueGenerator.GetOutputFileName(recipe)}",
                "hub-chip"));
            meta.Add(HubUiFactory.Badge(
                status.CanGenerate ? "可生成" : "条件未满足",
                status.CanGenerate ? "hub-badge--ready" : "hub-badge--pending"));
            header.Add(meta);

            var btnRow = new VisualElement();
            btnRow.AddToClassList("hub-btn-row");
            var genBtn = HubUiFactory.Button($"生成 {recipe.template}.g.cs", "hub-btn hub-btn--primary", () =>
            {
                try
                {
                    GlueGenerator.Generate(recipe, _recipes);
                    RefreshSidebar();
                    RefreshDetail();
                }
                catch (System.Exception ex) { Debug.LogError($"[Hub] {ex.Message}"); }
            });
            genBtn.SetEnabled(status.CanGenerate);
            HubColors.RefreshButton(genBtn, HubUiFactory.JoinClasses(genBtn));
            btnRow.Add(genBtn);
            HubShellLayout.ApplyBtnRow(btnRow);
            header.Add(btnRow);
            shell.Top.Add(header);

            shell.ScrollContent.Add(HubUiFactory.Label("生成条件", "hub-section-title"));

            var checklist = new VisualElement();
            checklist.AddToClassList("hub-checklist");
            HubShellLayout.ApplyChecklist(checklist);

            if (recipe.requires != null)
            {
                foreach (var req in recipe.requires)
                    checklist.Add(HubUiFactory.CheckItem(
                        PackageDetector.IsRequirementMet(req, _catalog), req));
            }

            if (recipe.dependsOnRecipes != null)
            {
                foreach (var dep in recipe.dependsOnRecipes)
                    checklist.Add(HubUiFactory.CheckItem(
                        !status.MissingRecipeOutputs.Contains(dep), $"前置: {dep}"));
            }

            shell.ScrollContent.Add(checklist);

            if (recipe.needsHeroType)
            {
                shell.ScrollContent.Add(HubUiFactory.Label("Hero 类型配置", "hub-section-title"));
                shell.ScrollContent.Add(HubUiFactory.Field("类型名", HubSettings.HeroType, v => HubSettings.HeroType = v));
                shell.ScrollContent.Add(HubUiFactory.Field("命名空间", HubSettings.HeroNamespace, v => HubSettings.HeroNamespace = v));
            }

            shell.ScrollContent.Add(HubUiFactory.Label("详细说明", "hub-section-title"));
            AddMarkdownCard(shell.ScrollContent, GlueRecipeDocs.GetDoc(recipe));

            if (!status.CanGenerate)
                shell.ScrollContent.Add(HubUiFactory.WarningBox("请先导入所需包，并生成前置 Recipe。"));
        }

        private void BuildProjectStructureSidebar()
        {
            if (_structure?.presets == null || _structure.presets.Length == 0)
            {
                _sidebarList.Add(HubUiFactory.Label("未找到 project-structure.json", "hub-empty-text"));
                return;
            }

            _sidebarList.Add(HubUiFactory.Label("结构预设", "hub-category-label"));

            foreach (var preset in _structure.presets)
            {
                var selected = preset.id == _selectedStructurePresetId;
                var captured = preset;

                var item = HubUiFactory.SidebarItem(selected, el =>
                {
                    var nameLbl = HubUiFactory.Label(preset.displayName, "hub-list-item-name");
                    HubColors.ApplyListItemName(nameLbl, selected);
                    el.Add(nameLbl);
                });

                item.RegisterCallback<ClickEvent>(_ =>
                {
                    _selectedStructurePresetId = captured.id;
                    HubSettings.SelectedStructurePresetId = captured.id;
                    RefreshSidebar();
                    RefreshDetail();
                });

                _sidebarList.Add(item);
            }

            _sidebarFooter.Add(HubUiFactory.Label(
                $"{_structure.presets.Length} 种预设 · 根目录可自定义",
                "hub-sidebar-hint"));
        }

        private void BuildProjectStructureDetail()
        {
            var preset = FindStructurePreset(_selectedStructurePresetId);
            if (preset == null)
            {
                ShowEmpty("← 选择一种结构预设", "精简 / 标准 / 完整");
                return;
            }

            var shell = CreateDetailShell(withBottom: true);

            shell.Top.Add(HubUiFactory.Label("一键生成项目结构", "hub-detail-title"));
            shell.Top.Add(HubUiFactory.Label(preset.description, "hub-detail-desc"));

            var meta = HubUiFactory.MetaRow();
            meta.Add(HubUiFactory.Label($"{preset.folders?.Length ?? 0} 个主目录", "hub-chip"));
            if (preset.extraRoots != null && preset.extraRoots.Length > 0)
                meta.Add(HubUiFactory.Label($"+{preset.extraRoots.Length} 扩展根", "hub-chip hub-chip--category"));
            shell.Top.Add(meta);

            shell.Top.Add(HubUiFactory.Label("生成选项", "hub-section-title"));
            shell.Top.Add(HubUiFactory.Field("根目录", HubSettings.ProjectRoot, v => HubSettings.ProjectRoot = v));
            shell.Top.Add(HubUiFactory.Field("项目名", HubSettings.ProjectDisplayName, v => HubSettings.ProjectDisplayName = v));
            shell.Top.Add(HubUiFactory.ToggleRow("写入 .gitkeep（空目录占位）", HubSettings.WriteGitKeep, v => HubSettings.WriteGitKeep = v));
            shell.Top.Add(HubUiFactory.ToggleRow("生成 README.md", HubSettings.WriteReadme, v => HubSettings.WriteReadme = v));
            shell.Top.Add(HubUiFactory.ToggleRow("生成 asmdef 样板", HubSettings.WriteAsmdefStubs, v => HubSettings.WriteAsmdefStubs = v));

            shell.Top.Add(HubUiFactory.Label("将创建的目录", "hub-section-title"));

            var tree = new VisualElement();
            tree.AddToClassList("hub-readme-card");
            HubColors.ApplyReadmeCard(tree);

            var rootDisplay = string.IsNullOrEmpty(HubSettings.ProjectRoot) ? preset.root : HubSettings.ProjectRoot;
            tree.Add(HubUiFactory.Label(rootDisplay + "/", "hub-tree-root"));

            if (preset.folders != null)
            {
                foreach (var folder in preset.folders)
                    tree.Add(HubUiFactory.Label("  ├─ " + folder + "/", "hub-tree-item"));
            }

            if (preset.extraRoots != null)
            {
                foreach (var extra in preset.extraRoots)
                {
                    if (extra?.folders == null) continue;
                    tree.Add(HubUiFactory.Label(extra.root + "/", "hub-tree-root"));
                    foreach (var folder in extra.folders)
                        tree.Add(HubUiFactory.Label("  ├─ " + folder + "/", "hub-tree-item"));
                }
            }

            shell.ScrollContent.Add(tree);

            var btnRow = new VisualElement();
            btnRow.AddToClassList("hub-btn-row");

            btnRow.Add(HubUiFactory.Button("一键生成", "hub-btn hub-btn--primary", () =>
            {
                if (!EditorUtility.DisplayDialog(
                        "生成项目结构",
                        $"预设：{preset.displayName}\n根目录：{HubSettings.ProjectRoot}\n\n已存在文件夹不会删除或覆盖。",
                        "生成",
                        "取消"))
                    return;

                try
                {
                    var result = ProjectStructureGenerator.Generate(preset, new ProjectStructureOptions
                    {
                        RootOverride = HubSettings.ProjectRoot,
                        WriteGitKeep = HubSettings.WriteGitKeep,
                        WriteReadme = HubSettings.WriteReadme,
                        WriteAsmdefStubs = HubSettings.WriteAsmdefStubs,
                        ProjectDisplayName = HubSettings.ProjectDisplayName
                    });

                    EditorUtility.DisplayDialog(
                        "完成",
                        $"新建文件夹：{result.FoldersCreated}\n跳过已存在：{result.FoldersSkipped}\n新建文件：{result.FilesCreated}",
                        "确定");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Hub] {ex.Message}");
                    EditorUtility.DisplayDialog("失败", ex.Message, "确定");
                }
            }));

            btnRow.Add(HubUiFactory.Button("在 Project 中定位", "hub-btn hub-btn--ghost", () =>
            {
                var folder = ProjectStructureGenerator.NormalizeAssetPath(HubSettings.ProjectRoot);
                var obj = AssetDatabase.LoadAssetAtPath<Object>(folder);
                if (obj != null)
                    EditorGUIUtility.PingObject(obj);
                else
                    Debug.LogWarning($"[Hub] 目录尚不存在: {folder}，请先生成。");
            }));

            HubShellLayout.ApplyBtnRow(btnRow);
            shell.Bottom.Add(btnRow);

            shell.Bottom.Add(HubUiFactory.WarningBox(
                "框架包放在 Assets/Package-TechCosmos/ 或 UPM Git 依赖（只读）。业务代码、Resources、Art 放在 _Game 下。"));
        }

        private void ShowEmpty(string title, string hint)
        {
            var empty = new VisualElement();
            empty.AddToClassList("hub-empty-state");
            HubColors.ApplyEmptyState(empty);
            empty.Add(HubUiFactory.Label(title, "hub-detail-title"));
            empty.Add(HubUiFactory.Label(hint, "hub-empty-text"));
            _detailPanel.Add(empty);
        }

        private static Label PresenceBadge(PackagePresence presence)
        {
            return presence switch
            {
                PackagePresence.InManifest => HubUiFactory.Badge("已导入 UPM", "hub-badge--installed"),
                PackagePresence.AssetsEmbedded => HubUiFactory.Badge("已嵌入 Assets", "hub-badge--installed"),
                PackagePresence.LocalOnly => HubUiFactory.Badge("本地源可用", "hub-badge--local"),
                _ => HubUiFactory.Badge("未导入", "hub-badge--missing")
            };
        }

        private void UpdatePackage(PackageCatalogEntry pkg)
        {
            try
            {
                PackageInstaller.UpdatePackage(pkg, _catalog);

                if (HubSettings.ImportMode == HubImportMode.GitUpm)
                {
                    if (!string.IsNullOrEmpty(pkg.gitUrl))
                        Client.Add($"{pkg.id}@{pkg.gitUrl}");
                    _pendingResolve = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("更新完成", $"{pkg.displayName} 已重新同步。", "确定");
                    EditorApplication.delayCall += ReloadData;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Hub] 更新失败: {ex.Message}");
                EditorUtility.DisplayDialog("更新失败", ex.Message, "确定");
            }
        }

        private void ImportPackage(PackageCatalogEntry pkg)
        {
            try
            {
                var result = PackageInstaller.ImportPackageWithDependencies(pkg, _catalog);
                if (HubSettings.ImportMode == HubImportMode.GitUpm)
                    _pendingResolve = true;

                if (result.Failed.Count > 0)
                {
                    EditorUtility.DisplayDialog("导入失败", string.Join("\n", result.Failed), "确定");
                    return;
                }

                if (result.Succeeded.Count > 1)
                {
                    EditorUtility.DisplayDialog(
                        "导入完成",
                        $"已导入 {result.Succeeded.Count} 个包（含依赖）：\n• " +
                        string.Join("\n• ", result.Succeeded),
                        "确定");
                }

                EditorApplication.delayCall += ReloadData;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Hub] 导入失败: {ex.Message}");
                EditorUtility.DisplayDialog("导入失败", ex.Message, "确定");
            }
        }

        private PackageCatalogEntry FindPackage(string id)
        {
            if (_catalog?.packages == null || string.IsNullOrEmpty(id)) return null;
            foreach (var p in _catalog.packages)
                if (p.id == id) return p;
            return null;
        }

        private GlueRecipeEntry FindRecipe(string id)
        {
            if (_recipes?.recipes == null || string.IsNullOrEmpty(id)) return null;
            foreach (var r in _recipes.recipes)
                if (r.id == id) return r;
            return null;
        }

        private ProjectStructurePreset FindStructurePreset(string id)
            => ProjectStructureGenerator.FindPreset(_structure, id);
    }
}
#endif
