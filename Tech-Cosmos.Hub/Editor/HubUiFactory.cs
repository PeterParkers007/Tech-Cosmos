#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    internal static class HubUiFactory
    {
        public static Label Label(string text, string className = null)
        {
            var l = new Label(text) { text = text };
            if (!string.IsNullOrEmpty(className)) l.AddToClassList(className);
            RouteLabelStyle(l, className);
            return l;
        }

        private static void RouteLabelStyle(Label l, string className)
        {
            if (string.IsNullOrEmpty(className)) return;

            if (className == "hub-title") HubColors.ApplyTitle(l);
            else if (className == "hub-subtitle") HubColors.ApplySubtitle(l);
            else if (className == "hub-section-title") HubColors.ApplySectionTitle(l);
            else if (className == "hub-detail-title") HubColors.ApplyDetailTitle(l);
            else if (className == "hub-detail-desc") HubColors.ApplyDetailDesc(l);
            else if (className == "hub-field-label") HubColors.ApplyFieldLabel(l);
            else if (className == "hub-field-label-stack") HubColors.ApplyFieldLabel(l, stackAbove: true);
            else if (className == "hub-sidebar-hint") HubColors.ApplySidebarHint(l);
            else if (className == "hub-category-label") HubColors.ApplyCategoryLabel(l);
            else if (className == "hub-empty-text") HubColors.ApplyEmptyText(l);
            else if (className == "hub-warning-text") HubColors.ApplyWarningText(l);
            else if (className == "hub-dep-heading") HubColors.ApplyDepHeading(l);
            else if (className == "hub-dep-detail") HubColors.ApplyDepDetail(l);
            else if (className == "hub-dep-block-reason") HubColors.ApplyDepBlockReason(l);
            else if (className == "hub-tree-root") HubColors.ApplyTreeRoot(l);
            else if (className == "hub-tree-item") HubColors.ApplyTreeItem(l);
            else if (className.Contains("hub-chip--category")) HubColors.ApplyChip(l, category: true);
            else if (className.Contains("hub-chip")) HubColors.ApplyChip(l);
            else if (className == "hub-git-version-title") HubColors.ApplyGitVersionTitle(l);
            else if (className == "hub-git-version-count") HubColors.ApplyGitVersionCount(l);
            else if (className == "hub-git-version-hint") HubColors.ApplyGitVersionHint(l);
            else if (className == "hub-git-version-value-primary") HubColors.ApplyGitVersionValuePrimary(l, latest: l.userData as string == HubGitVersion.LatestLabel);
            else if (className == "hub-git-version-value-sub") HubColors.ApplyGitVersionValueSub(l);
            else if (className == "hub-git-version-chevron") HubColors.ApplyGitVersionChevron(l);
            else if (className == "hub-git-version-loading-text") HubColors.ApplyGitVersionLoadingText(l);
            else if (className.Contains("hub-list-item-name"))
            {
                // selected state applied separately via ApplyListItemName
                l.style.fontSize = 12;
                l.style.color = HubColors.BodyText;
                l.style.flexGrow = 1;
            }
        }

        public static VisualElement MetaRow()
        {
            var meta = new VisualElement();
            meta.AddToClassList("hub-detail-meta");
            HubShellLayout.ApplyDetailMeta(meta);
            return meta;
        }

        public static Button Button(string text, string className, System.Action onClick)
        {
            var b = new Button(onClick) { text = text };
            if (!string.IsNullOrEmpty(className))
            {
                foreach (var part in className.Split(' '))
                {
                    if (!string.IsNullOrEmpty(part))
                        b.AddToClassList(part);
                }
            }

            StyleButton(b, className);
            return b;
        }

        /// <summary>
        /// 统一为 Hub 内所有 Button 绑定样式与 hover/press 反馈。
        /// </summary>
        public static void StyleButton(Button button, string className = null, bool? tabActive = null)
        {
            if (button == null) return;

            className ??= JoinClasses(button);
            if (string.IsNullOrEmpty(className)) return;

            if (button.ClassListContains("hub-btn"))
            {
                HubColors.RefreshButton(button, className);
                return;
            }

            if (button.ClassListContains("hub-dep-name"))
            {
                HubColors.RefreshDepLinkButton(button);
                return;
            }

            if (button.ClassListContains("hub-mode-toggle") || button.ClassListContains("hub-import-mode-toggle"))
            {
                var active = tabActive ?? button.ClassListContains("hub-tab--active");
                HubColors.RefreshModeToggle(button, active);
                return;
            }

            if (button.ClassListContains("hub-tab"))
            {
                var active = tabActive ?? button.ClassListContains("hub-tab--active");
                HubColors.RefreshTab(button, active);
            }
        }

        /// <summary>
        /// 扫描子树，为所有可点击控件补全 hover/press 绑定（防止遗漏）。
        /// </summary>
        public static void ApplyInteractionFeedback(VisualElement root)
        {
            if (root == null) return;

            root.Query<Button>().ForEach(b => StyleButton(b));
            root.Query<Toggle>().ForEach(t => HubColors.RefreshToggle(t));
            root.Query(className: "hub-list-item").ForEach(item =>
                HubColors.RefreshListItem(item, item.ClassListContains("hub-list-item--selected")));
            root.Query(className: "hub-md-link").ForEach(label =>
            {
                if (label is Label linkLabel)
                    HubColors.RefreshMarkdownLink(linkLabel);
            });
        }

        public static string JoinClasses(VisualElement element)
        {
            if (element == null) return string.Empty;
            return string.Join(" ", element.GetClasses());
        }

        private static Button ModeToggleButton(string text, string elementName, System.Action onClick)
        {
            var b = new Button(onClick) { text = text, name = elementName };
            b.AddToClassList("hub-tab");
            b.AddToClassList("hub-mode-toggle");
            return b;
        }

        public static VisualElement StatusDot(PackagePresence presence)
        {
            var dot = new VisualElement();
            dot.AddToClassList("hub-status-dot");
            var installed = presence is PackagePresence.InManifest or PackagePresence.AssetsEmbedded;
            HubColors.ApplyStatusDot(dot, installed, presence == PackagePresence.LocalOnly);
            return dot;
        }

        public static VisualElement DependencyLinkRow(
            PackageDependencyLink link,
            bool showArrowIn,
            System.Action<string> onSelectPackageId = null)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-dep-row");
            HubShellLayout.ApplyDepRow(row);

            if (showArrowIn)
                row.Add(Label("←", "hub-dep-arrow"));

            var nameBtn = Button(link.DisplayName ?? link.PackageId, "hub-dep-name", () =>
            {
                onSelectPackageId?.Invoke(link.PackageId);
            });
            nameBtn.tooltip = link.PackageId;
            row.Add(nameBtn);

            var badgeClass = link.State switch
            {
                PackageDependencyState.Installed => "hub-badge--installed",
                PackageDependencyState.PendingImport => "hub-badge--pending",
                PackageDependencyState.Blocked => "hub-badge--missing",
                _ => "hub-badge--missing"
            };
            var stateLabel = StateLabel(link.State);
            row.Add(Badge(stateLabel, badgeClass));

            if (!string.IsNullOrEmpty(link.Detail) && link.Detail != stateLabel)
                row.Add(Label(link.Detail, "hub-dep-detail"));

            if (!showArrowIn)
                row.Add(Label("→", "hub-dep-arrow"));

            return row;
        }

        private static string StateLabel(PackageDependencyState state) => state switch
        {
            PackageDependencyState.Installed => "已导入",
            PackageDependencyState.PendingImport => "待导入",
            PackageDependencyState.Blocked => "不可导入",
            _ => "未收录"
        };

        public static VisualElement DependencySection(
            PackageImportPlan plan,
            System.Action<string> onSelectPackageId)
        {
            var section = new VisualElement();
            section.AddToClassList("hub-dep-section");
            HubColors.ApplyDepSection(section);

            if (!plan.CanImport && !string.IsNullOrEmpty(plan.BlockReason))
                section.Add(Label(plan.BlockReason, "hub-dep-block-reason"));

            if (plan.DependsOn.Count > 0)
            {
                section.Add(Label("依赖于（需先拥有）", "hub-dep-heading"));
                var up = new VisualElement();
                up.AddToClassList("hub-dep-list");
                HubShellLayout.ApplyDepList(up);
                foreach (var link in plan.DependsOn)
                    up.Add(DependencyLinkRow(link, showArrowIn: true, onSelectPackageId));
                section.Add(up);
            }

            if (plan.DependedBy.Count > 0)
            {
                section.Add(Label("被依赖于", "hub-dep-heading"));
                var down = new VisualElement();
                down.AddToClassList("hub-dep-list");
                HubShellLayout.ApplyDepList(down);
                foreach (var link in plan.DependedBy)
                    down.Add(DependencyLinkRow(link, showArrowIn: false, onSelectPackageId));
                section.Add(down);
            }

            if (plan.DependsOn.Count == 0 && plan.DependedBy.Count == 0)
                section.Add(Label("catalog 中未配置依赖关系。", "hub-dep-empty"));

            return section;
        }

        public static Label Badge(string text, string badgeClass)
        {
            var b = Label(text, "hub-badge " + badgeClass);
            HubColors.ApplyBadge(b, badgeClass);
            return b;
        }

        public static VisualElement CheckItem(bool ok, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-check-item");
            var icon = Label(ok ? "✓" : "✗", ok ? "hub-check-icon hub-check-icon--ok" : "hub-check-icon hub-check-icon--fail");
            var label = Label(text, "hub-check-label");
            HubColors.ApplyCheckItem(row, icon, label, ok);
            row.Add(icon);
            row.Add(label);
            return row;
        }

        public static VisualElement Field(string label, string value, System.Action<string> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-field-row");
            HubShellLayout.ApplyFieldRow(row);
            row.Add(Label(label, "hub-field-label"));

            var field = new TextField { value = value };
            field.AddToClassList("hub-field-input");
            HubColors.ApplyTextField(field);
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            row.Add(field);
            return row;
        }

        public static VisualElement ImportModeSelector(System.Action refresh)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-import-mode");
            HubShellLayout.ApplyImportMode(row);

            row.Add(Label("导入模式", "hub-field-label-stack"));

            var btnRow = new VisualElement();
            btnRow.AddToClassList("hub-import-mode-row");
            HubShellLayout.ApplyImportModeRow(btnRow);

            var gitBtn = ModeToggleButton("Git UPM", "hub-mode-git", () =>
            {
                if (HubSettings.ImportMode == HubImportMode.GitUpm) return;
                HubSettings.ImportMode = HubImportMode.GitUpm;
                UpdateImportModeButtons(btnRow);
                refresh?.Invoke();
            });

            var assetsBtn = ModeToggleButton("Assets 嵌入", "hub-mode-assets", () =>
            {
                if (HubSettings.ImportMode == HubImportMode.AssetsEmbed) return;
                HubSettings.ImportMode = HubImportMode.AssetsEmbed;
                UpdateImportModeButtons(btnRow);
                refresh?.Invoke();
            });

            btnRow.Add(gitBtn);
            btnRow.Add(assetsBtn);
            row.Add(btnRow);
            row.Add(Label(GetImportModeHint(), "hub-sidebar-hint"));

            UpdateImportModeButtons(btnRow);
            return row;
        }

        private static string GetImportModeHint()
        {
            return HubSettings.ImportMode == HubImportMode.AssetsEmbed
                ? HubSettings.AssetsPackageRoot
                : "Packages/manifest.json";
        }

        private static void UpdateImportModeButtons(VisualElement btnRow)
        {
            var git = btnRow.Q<Button>("hub-mode-git");
            var assets = btnRow.Q<Button>("hub-mode-assets");
            if (git != null)
            {
                git.EnableInClassList("hub-tab--active", HubSettings.ImportMode == HubImportMode.GitUpm);
                HubColors.RefreshModeToggle(git, HubSettings.ImportMode == HubImportMode.GitUpm);
            }
            if (assets != null)
            {
                assets.EnableInClassList("hub-tab--active", HubSettings.ImportMode == HubImportMode.AssetsEmbed);
                HubColors.RefreshModeToggle(assets, HubSettings.ImportMode == HubImportMode.AssetsEmbed);
            }
        }

        public static VisualElement ToggleRow(string label, bool value, System.Action<bool> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-field-row");
            HubShellLayout.ApplyFieldRow(row);

            var toggle = new Toggle(label) { value = value };
            toggle.AddToClassList("hub-toggle");
            toggle.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            HubColors.RefreshToggle(toggle);
            row.Add(toggle);
            return row;
        }

        public static VisualElement GitVersionSelector(PackageCatalogEntry entry, Action onChanged = null)
        {
            var card = new VisualElement();
            card.AddToClassList("hub-git-version-card");
            HubShellLayout.ApplyGitVersionCard(card);
            HubColors.ApplyGitVersionCard(card);

            var header = new VisualElement();
            header.AddToClassList("hub-git-version-header");
            HubShellLayout.ApplyGitVersionHeader(header);
            header.Add(Label("Git 版本", "hub-git-version-title"));

            var countBadge = Label("…", "hub-git-version-count");
            countBadge.name = "git-version-count";
            header.Add(countBadge);
            card.Add(header);

            var body = new VisualElement();
            body.name = "git-version-body";
            body.AddToClassList("hub-git-version-body");
            card.Add(body);
            ShowGitVersionLoading(body);

            var gitUrl = entry.gitUrl;
            System.Threading.Tasks.Task.Run(() => HubGitVersion.FetchTags(gitUrl))
                .ContinueWith(task =>
                {
                    var tags = task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion
                        ? task.Result
                        : new List<string>();
                    EditorApplication.delayCall += () =>
                        PopulateGitVersionBody(body, countBadge, entry, tags, onChanged);
                });

            return card;
        }

        private static void ShowGitVersionLoading(VisualElement body)
        {
            body.Clear();
            var row = new VisualElement();
            row.AddToClassList("hub-git-version-loading");
            HubColors.ApplyGitVersionLoading(row);

            var dot = new VisualElement();
            dot.AddToClassList("hub-git-version-loading-dot");
            HubColors.ApplyGitVersionLoadingDot(dot);
            row.Add(dot);
            row.Add(Label("正在获取远程标签…", "hub-git-version-loading-text"));
            body.Add(row);
        }

        private static void PopulateGitVersionBody(
            VisualElement body,
            Label countBadge,
            PackageCatalogEntry entry,
            List<string> tags,
            Action onChanged)
        {
            if (body == null || entry == null)
                return;

            body.Clear();
            body.userData = tags;

            var choices = BuildGitVersionChoices(tags);
            var selectedLabel = ResolveSelectedLabel(entry, choices);
            UpdateGitVersionCountBadge(countBadge, tags);

            var trigger = CreateGitVersionTrigger(entry, choices, selectedLabel, onChanged);
            body.Add(trigger);

            var footer = new VisualElement();
            footer.AddToClassList("hub-git-version-footer");
            HubShellLayout.ApplyGitVersionFooter(footer);

            if (tags.Count == 0)
                footer.Add(Label("远程仓库暂无标签，将使用默认分支最新提交", "hub-git-version-hint"));

            if (ManifestHelper.TryGetDependency(entry.id, out var source))
            {
                var installed = HubGitVersion.TryParseTag(source, out var installedTag) && !string.IsNullOrEmpty(installedTag)
                    ? installedTag
                    : "最新";
                footer.Add(Badge($"已安装 · {installed}", "hub-badge--installed"));
            }

            if (footer.childCount > 0)
                body.Add(footer);
        }

        private static List<string> BuildGitVersionChoices(List<string> tags)
        {
            var choices = new List<string> { HubGitVersion.LatestLabel };
            choices.AddRange(tags);
            return choices;
        }

        private static string ResolveSelectedLabel(PackageCatalogEntry entry, List<string> choices)
        {
            var effectiveTag = HubGitVersion.GetEffectiveSelectedTag(entry.id);
            var selectedLabel = string.IsNullOrEmpty(effectiveTag)
                ? HubGitVersion.LatestLabel
                : effectiveTag;

            if (choices.Exists(c => string.Equals(c, selectedLabel, StringComparison.OrdinalIgnoreCase)))
                return selectedLabel;

            if (!string.IsNullOrEmpty(effectiveTag))
            {
                choices.Insert(1, effectiveTag);
                return effectiveTag;
            }

            return HubGitVersion.LatestLabel;
        }

        private static void UpdateGitVersionCountBadge(Label countBadge, List<string> tags)
        {
            if (countBadge == null)
                return;

            countBadge.text = tags.Count > 0 ? $"{tags.Count} 个标签" : "无标签";
        }

        private static Button CreateGitVersionTrigger(
            PackageCatalogEntry entry,
            List<string> choices,
            string selectedLabel,
            Action onChanged)
        {
            var trigger = new Button { text = "" };
            trigger.AddToClassList("hub-git-version-trigger");
            trigger.name = "git-version-trigger";
            HubColors.RefreshGitVersionTrigger(trigger);

            var content = new VisualElement();
            content.AddToClassList("hub-git-version-trigger-content");
            HubShellLayout.ApplyGitVersionTriggerContent(content);

            var icon = Label(GetGitVersionIcon(selectedLabel), "hub-git-version-icon");
            HubColors.ApplyGitVersionIcon(icon, IsLatestChoice(selectedLabel));
            content.Add(icon);

            var valueBlock = new VisualElement();
            HubShellLayout.ApplyGitVersionValueBlock(valueBlock);
            var primary = Label(GetGitVersionPrimary(selectedLabel), "hub-git-version-value-primary");
            primary.userData = selectedLabel;
            HubColors.ApplyGitVersionValuePrimary(primary, IsLatestChoice(selectedLabel));
            valueBlock.Add(primary);

            var secondaryText = GetGitVersionSecondary(selectedLabel);
            if (!string.IsNullOrEmpty(secondaryText))
                valueBlock.Add(Label(secondaryText, "hub-git-version-value-sub"));

            content.Add(valueBlock);
            content.Add(Label("▾", "hub-git-version-chevron"));
            trigger.Add(content);

            trigger.clicked += () => ShowGitVersionMenu(trigger, entry, choices, selectedLabel, onChanged);
            return trigger;
        }

        private static void ShowGitVersionMenu(
            Button trigger,
            PackageCatalogEntry entry,
            List<string> choices,
            string selectedLabel,
            Action onChanged)
        {
            var menu = new GenericDropdownMenu();
            foreach (var choice in choices)
            {
                var itemLabel = FormatGitVersionMenuItem(choice);
                var isSelected = string.Equals(choice, selectedLabel, StringComparison.OrdinalIgnoreCase);
                var captured = choice;
                menu.AddItem(itemLabel, isSelected, () =>
                {
                    var tag = captured == HubGitVersion.LatestLabel ? "" : captured;
                    HubGitVersion.SetSelectedTag(entry.id, tag);
                    onChanged?.Invoke();

                    var body = trigger.parent;
                    var countBadge = body?.parent?.Q<Label>("git-version-count");
                    var tags = body?.userData as List<string> ?? new List<string>();
                    if (body != null)
                        PopulateGitVersionBody(body, countBadge, entry, tags, onChanged);
                });
            }

            menu.DropDown(trigger.worldBound, trigger);
        }

        private static bool IsLatestChoice(string choice)
            => choice == HubGitVersion.LatestLabel;

        private static string GetGitVersionIcon(string choice)
            => IsLatestChoice(choice) ? "◎" : "⎇";

        private static string GetGitVersionPrimary(string choice)
            => IsLatestChoice(choice) ? "最新" : choice;

        private static string GetGitVersionSecondary(string choice)
            => IsLatestChoice(choice) ? "main / HEAD" : null;

        private static string FormatGitVersionMenuItem(string choice)
            => IsLatestChoice(choice) ? "最新  ·  main / HEAD" : choice;

        public static VisualElement WarningBox(string text)
        {
            var box = new VisualElement();
            box.AddToClassList("hub-warning-box");
            HubColors.ApplyWarningBox(box);
            box.Add(Label(text, "hub-warning-text"));
            return box;
        }

        public static VisualElement SidebarItem(bool selected, System.Action<VisualElement> buildContent)
        {
            var item = new VisualElement();
            item.AddToClassList("hub-list-item");
            if (selected) item.AddToClassList("hub-list-item--selected");
            HubShellLayout.ApplyListItem(item);
            HubColors.RefreshListItem(item, selected);
            buildContent?.Invoke(item);
            return item;
        }
    }
}
#endif
