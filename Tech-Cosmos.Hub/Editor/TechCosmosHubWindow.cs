#if UNITY_EDITOR
using System.Collections.Generic;
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

        [MenuItem("Tech-Cosmos/Hub", priority = 0)]
        public static void Open()
        {
            var w = GetWindow<TechCosmosHubWindow>("Tech-Cosmos Hub");
            w.minSize = new Vector2(640, 420);
            w.Show();
        }

        private void CreateGUI()
        {
            LoadStylesheet();

            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            _root = new VisualElement();
            _root.AddToClassList("hub-root");
            _root.style.flexGrow = 1;
            rootVisualElement.Add(_root);

            BuildHeader();
            BuildTabs();
            BuildBody();

            _root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            ReloadData();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void LoadStylesheet()
        {
            var direct = $"{HubPaths.HubAssetPath}/Editor/UI/TechCosmosHub.uss";
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(direct);
            if (sheet == null)
            {
                var guids = AssetDatabase.FindAssets("TechCosmosHub t:StyleSheet");
                if (guids.Length > 0)
                    sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (sheet != null)
                rootVisualElement.styleSheets.Add(sheet);
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
            _catalog = HubDataLoader.LoadCatalog();
            _recipes = HubDataLoader.LoadRecipes();
            _structure = HubDataLoader.LoadProjectStructure();

            if (string.IsNullOrEmpty(_selectedPackageId) && _catalog?.packages?.Length > 0)
                _selectedPackageId = _catalog.packages[0].id;

            if (string.IsNullOrEmpty(_selectedRecipeId) && _recipes?.recipes?.Length > 0)
                _selectedRecipeId = _recipes.recipes[0].id;

            _selectedStructurePresetId = HubSettings.SelectedStructurePresetId;
            if (ProjectStructureGenerator.FindPreset(_structure, _selectedStructurePresetId) == null &&
                _structure?.presets?.Length > 0)
                _selectedStructurePresetId = _structure.presets[0].id;

            RefreshStats();
            RefreshSidebar();
            RefreshDetail();
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (_body == null || _header == null) return;
            var w = evt.newRect.width;
            _body.EnableInClassList("hub-body--narrow", w < 760);
            _header.EnableInClassList("hub-header--compact", w < 920);
            _header.EnableInClassList("hub-header--stacked", w < 640);
        }

        private sealed class DetailShell
        {
            public VisualElement Top;
            public ScrollView Scroll;
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
            _detailPanel.Add(shell.Scroll);

            if (withBottom)
            {
                shell.Bottom = new VisualElement();
                shell.Bottom.AddToClassList("hub-detail-bottom");
                _detailPanel.Add(shell.Bottom);
            }

            return shell;
        }

        private static void AddReadmeCard(ScrollView scroll, string text)
        {
            var card = new VisualElement();
            card.AddToClassList("hub-readme-card");
            card.Add(HubUiFactory.Label(text, "hub-readme-text"));
            scroll.Add(card);
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

            _root.Add(_header);
        }

        private static Label AddStatChip(VisualElement parent, string value, string label)
        {
            var chip = new VisualElement();
            chip.AddToClassList("hub-stat-chip");
            var val = HubUiFactory.Label(value, "hub-stat-value");
            chip.Add(val);
            chip.Add(HubUiFactory.Label(label, "hub-stat-label"));
            parent.Add(chip);
            return val;
        }

        private void BuildTabs()
        {
            var tabs = new VisualElement();
            tabs.AddToClassList("hub-tabs");

            _tabPackages = HubUiFactory.Button("技术框架", "hub-tab hub-tab--active", () => SetTab(HubTab.Packages));
            _tabGlue = HubUiFactory.Button("胶水生成", "hub-tab", () => SetTab(HubTab.Glue));
            _tabProjectStructure = HubUiFactory.Button("项目结构", "hub-tab", () => SetTab(HubTab.ProjectStructure));

            tabs.Add(_tabPackages);
            tabs.Add(_tabGlue);
            tabs.Add(_tabProjectStructure);
            _root.Add(tabs);
        }

        private void SetTab(HubTab tab)
        {
            _tab = tab;
            _tabPackages.EnableInClassList("hub-tab--active", tab == HubTab.Packages);
            _tabGlue.EnableInClassList("hub-tab--active", tab == HubTab.Glue);
            _tabProjectStructure.EnableInClassList("hub-tab--active", tab == HubTab.ProjectStructure);

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

            _searchField = new ToolbarSearchField();
            _searchField.AddToClassList("hub-sidebar-search");
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? string.Empty;
                RefreshSidebar();
            });
            sidebar.Add(_searchField);

            var sidebarScroll = new ScrollView(ScrollViewMode.Vertical);
            sidebarScroll.AddToClassList("hub-sidebar-scroll");
            _sidebarList = new VisualElement();
            sidebarScroll.Add(_sidebarList);
            sidebar.Add(sidebarScroll);

            var sidebarFooter = new VisualElement();
            sidebarFooter.AddToClassList("hub-sidebar-footer");
            sidebar.Add(sidebarFooter);
            _sidebarFooter = sidebarFooter;

            _detailPanel = new VisualElement();
            _detailPanel.AddToClassList("hub-detail");

            _body.Add(sidebar);
            _body.Add(_detailPanel);
            _root.Add(_body);
        }

        private VisualElement _sidebarFooter;

        private void RefreshStats()
        {
            if (_catalog?.packages == null) return;

            int installed = 0, local = 0;
            foreach (var p in _catalog.packages)
            {
                var presence = PackageDetector.GetPresence(p, _catalog);
                if (presence == PackagePresence.InManifest) installed++;
                else if (presence == PackagePresence.LocalOnly) local++;
            }

            _statInstalled.text = installed.ToString();
            _statLocal.text = local.ToString();
            _statTotal.text = _catalog.packages.Length.ToString();
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
        }

        private void BuildPackageSidebar()
        {
            if (_catalog?.packages == null || _catalog.packages.Length == 0)
            {
                _sidebarList.Add(HubUiFactory.Label("未找到 package-catalog.json", "hub-empty-text"));
                return;
            }

            var filtered = _catalog.packages
                .Where(MatchesSearch)
                .GroupBy(p => p.category ?? "Other")
                .OrderBy(g => g.Key);

            foreach (var group in filtered)
            {
                _sidebarList.Add(HubUiFactory.Label(group.Key.ToUpperInvariant(), "hub-category-label"));

                foreach (var pkg in group.OrderBy(p => p.displayName))
                {
                    var presence = PackageDetector.GetPresence(pkg, _catalog);
                    var item = new VisualElement();
                    item.AddToClassList("hub-list-item");
                    if (pkg.id == _selectedPackageId)
                        item.AddToClassList("hub-list-item--selected");

                    item.Add(HubUiFactory.StatusDot(presence));
                    item.Add(HubUiFactory.Label(pkg.displayName, "hub-list-item-name"));

                    var captured = pkg;
                    item.RegisterCallback<ClickEvent>(_ =>
                    {
                        _selectedPackageId = captured.id;
                        RefreshSidebar();
                        RefreshDetail();
                    });

                    _sidebarList.Add(item);
                }
            }

            var visible = _catalog.packages.Count(MatchesSearch);
            _sidebarFooter.Add(HubUiFactory.Label($"{visible} / {_catalog.packages.Length} 项", "hub-sidebar-hint"));
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
                var item = new VisualElement();
                item.AddToClassList("hub-list-item");
                if (recipe.id == _selectedRecipeId)
                    item.AddToClassList("hub-list-item--selected");

                var dot = new VisualElement();
                dot.AddToClassList("hub-status-dot");
                dot.AddToClassList(status.CanGenerate ? "hub-status-dot--installed" : "hub-status-dot--missing");
                item.Add(dot);
                item.Add(HubUiFactory.Label(recipe.displayName, "hub-list-item-name"));

                var captured = recipe;
                item.RegisterCallback<ClickEvent>(_ =>
                {
                    _selectedRecipeId = captured.id;
                    RefreshSidebar();
                    RefreshDetail();
                });

                _sidebarList.Add(item);
            }

            _sidebarFooter.Add(HubUiFactory.Button("生成全部就绪项", "hub-btn hub-btn--accent", () =>
            {
                GlueGenerator.GenerateAllReady(_catalog, _recipes);
                RefreshSidebar();
                RefreshDetail();
            }));
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
            var shell = CreateDetailShell();

            var header = new VisualElement();
            header.AddToClassList("hub-detail-header");

            var titleBlock = new VisualElement();
            titleBlock.AddToClassList("hub-detail-title-block");
            titleBlock.Add(HubUiFactory.Label(pkg.displayName, "hub-detail-title"));
            titleBlock.Add(HubUiFactory.Label(pkg.description, "hub-detail-desc"));

            var meta = new VisualElement();
            meta.AddToClassList("hub-detail-meta");
            meta.Add(HubUiFactory.Label(pkg.category, "hub-chip hub-chip--category"));
            meta.Add(HubUiFactory.Label(pkg.id, "hub-chip"));
            meta.Add(PresenceBadge(presence));
            titleBlock.Add(meta);
            header.Add(titleBlock);
            shell.Top.Add(header);

            var btnRow = new VisualElement();
            btnRow.AddToClassList("hub-btn-row");

            var importBtn = HubUiFactory.Button("导入包", "hub-btn hub-btn--primary", () => ImportPackage(pkg));
            importBtn.SetEnabled(presence != PackagePresence.NotAvailable || !string.IsNullOrEmpty(pkg.gitUrl));
            btnRow.Add(importBtn);

            var removeBtn = HubUiFactory.Button("从 Manifest 移除", "hub-btn hub-btn--danger", () =>
            {
                PackageInstaller.RemoveFromManifest(pkg.id);
                _pendingResolve = true;
                EditorApplication.delayCall += ReloadData;
            });
            removeBtn.SetEnabled(presence == PackagePresence.InManifest);
            btnRow.Add(removeBtn);

            var readmePath = PackageReadmeLoader.GetReadmeAssetPath(pkg, _catalog);
            var openBtn = HubUiFactory.Button("打开 README", "hub-btn hub-btn--ghost", () =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(readmePath);
                if (asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                    EditorGUIUtility.PingObject(asset);
                }
            });
            openBtn.SetEnabled(!string.IsNullOrEmpty(readmePath));
            btnRow.Add(openBtn);

            shell.Top.Add(btnRow);
            shell.Top.Add(HubUiFactory.Label("介绍 / README", "hub-section-title"));
            AddReadmeCard(shell.Scroll, PackageReadmeLoader.LoadPreview(pkg, _catalog));
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

            var meta = new VisualElement();
            meta.AddToClassList("hub-detail-meta");
            meta.Add(HubUiFactory.Label(
                $"输出 → {_recipes.outputRoot}/{GlueGenerator.GetOutputFileName(recipe.template)}",
                "hub-chip"));
            meta.Add(HubUiFactory.Label(
                status.CanGenerate ? "可生成" : "条件未满足",
                status.CanGenerate ? "hub-badge hub-badge--ready" : "hub-badge hub-badge--pending"));
            shell.Top.Add(meta);

            shell.Scroll.Add(HubUiFactory.Label("生成条件", "hub-section-title"));

            var checklist = new VisualElement();
            checklist.AddToClassList("hub-checklist");

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

            shell.Scroll.Add(checklist);

            if (recipe.needsHeroType)
            {
                shell.Scroll.Add(HubUiFactory.Label("Hero 类型配置", "hub-section-title"));
                shell.Scroll.Add(HubUiFactory.Field("类型名", HubSettings.HeroType, v => HubSettings.HeroType = v));
                shell.Scroll.Add(HubUiFactory.Field("命名空间", HubSettings.HeroNamespace, v => HubSettings.HeroNamespace = v));
            }

            shell.Scroll.Add(HubUiFactory.Label("详细说明", "hub-section-title"));
            AddReadmeCard(shell.Scroll, GlueRecipeDocs.GetDoc(recipe.id));

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
            btnRow.Add(genBtn);
            shell.Top.Add(btnRow);

            if (!status.CanGenerate)
            {
                var warn = new VisualElement();
                warn.AddToClassList("hub-warning-box");
                warn.Add(HubUiFactory.Label("请先导入所需包，并生成前置 Recipe。", "hub-warning-text"));
                shell.Top.Add(warn);
            }
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
                var item = new VisualElement();
                item.AddToClassList("hub-list-item");
                if (preset.id == _selectedStructurePresetId)
                    item.AddToClassList("hub-list-item--selected");

                item.Add(HubUiFactory.Label(preset.displayName, "hub-list-item-name"));

                var captured = preset;
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

            var meta = new VisualElement();
            meta.AddToClassList("hub-detail-meta");
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

            shell.Scroll.Add(tree);

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

            shell.Bottom.Add(btnRow);

            var warn = new VisualElement();
            warn.AddToClassList("hub-warning-box");
            warn.Add(HubUiFactory.Label(
                "Framework 包请保持在 Assets/Framework/（只读）。业务代码、Resources、Art 放在 _Game 下。",
                "hub-warning-text"));
            shell.Bottom.Add(warn);
        }

        private void ShowEmpty(string title, string hint)
        {
            var empty = new VisualElement();
            empty.AddToClassList("hub-empty-state");
            empty.Add(HubUiFactory.Label(title, "hub-detail-title"));
            empty.Add(HubUiFactory.Label(hint, "hub-empty-text"));
            _detailPanel.Add(empty);
        }

        private static Label PresenceBadge(PackagePresence presence)
        {
            return presence switch
            {
                PackagePresence.InManifest => HubUiFactory.Badge("已导入 UPM", "hub-badge--installed"),
                PackagePresence.LocalOnly => HubUiFactory.Badge("本地可用", "hub-badge--local"),
                _ => HubUiFactory.Badge("未导入", "hub-badge--missing")
            };
        }

        private void ImportPackage(PackageCatalogEntry pkg)
        {
            try
            {
                PackageInstaller.ImportPackage(pkg, _catalog);
                _pendingResolve = true;
                EditorApplication.delayCall += ReloadData;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Hub] 导入失败: {ex.Message}");
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
