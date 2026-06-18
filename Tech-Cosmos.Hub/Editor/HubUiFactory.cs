#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    internal static class HubUiFactory
    {
        public static Label Label(string text, string className = null)
        {
            var l = new Label(text) { text = text };
            if (!string.IsNullOrEmpty(className)) l.AddToClassList(className);
            return l;
        }

        public static Button Button(string text, string className, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            if (!string.IsNullOrEmpty(className)) b.AddToClassList(className);
            return b;
        }

        public static VisualElement StatusDot(PackagePresence presence)
        {
            var dot = new VisualElement();
            dot.AddToClassList("hub-status-dot");
            dot.AddToClassList(presence switch
            {
                PackagePresence.InManifest => "hub-status-dot--installed",
                PackagePresence.AssetsEmbedded => "hub-status-dot--installed",
                PackagePresence.LocalOnly => "hub-status-dot--local",
                _ => "hub-status-dot--missing"
            });
            return dot;
        }

        public static VisualElement DependencyLinkRow(
            PackageDependencyLink link,
            bool showArrowIn,
            Action<string> onSelectPackageId = null)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-dep-row");
            row.style.flexShrink = 0;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            if (showArrowIn)
            {
                var arrow = Label("←", "hub-dep-arrow");
                row.Add(arrow);
            }

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
            row.Add(Badge(StateLabel(link.State), "hub-badge " + badgeClass));

            if (!string.IsNullOrEmpty(link.Detail))
                row.Add(Label(link.Detail, "hub-dep-detail"));

            if (!showArrowIn)
            {
                var arrow = Label("→", "hub-dep-arrow");
                row.Add(arrow);
            }

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
            Action<string> onSelectPackageId)
        {
            var section = new VisualElement();
            section.AddToClassList("hub-dep-section");
            section.style.flexShrink = 0;
            section.style.flexDirection = FlexDirection.Column;

            if (!plan.CanImport && !string.IsNullOrEmpty(plan.BlockReason))
            {
                var warn = Label(plan.BlockReason, "hub-dep-block-reason");
                section.Add(warn);
            }

            if (plan.DependsOn.Count > 0)
            {
                section.Add(Label("依赖于（需先拥有）", "hub-dep-heading"));
                var up = new VisualElement();
                up.AddToClassList("hub-dep-list");
                up.style.flexDirection = FlexDirection.Column;
                up.style.flexShrink = 0;
                foreach (var link in plan.DependsOn)
                    up.Add(DependencyLinkRow(link, showArrowIn: true, onSelectPackageId));
                section.Add(up);
            }

            if (plan.DependedBy.Count > 0)
            {
                section.Add(Label("被依赖于", "hub-dep-heading"));
                var down = new VisualElement();
                down.AddToClassList("hub-dep-list");
                down.style.flexDirection = FlexDirection.Column;
                down.style.flexShrink = 0;
                foreach (var link in plan.DependedBy)
                    down.Add(DependencyLinkRow(link, showArrowIn: false, onSelectPackageId));
                section.Add(down);
            }

            if (plan.DependsOn.Count == 0 && plan.DependedBy.Count == 0)
                section.Add(Label("catalog 中未配置依赖关系。", "hub-dep-empty"));

            return section;
        }

        public static Label Badge(string text, string className)
        {
            var b = Label(text, "hub-badge " + className);
            return b;
        }

        public static VisualElement CheckItem(bool ok, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-check-item");

            var icon = Label(ok ? "✓" : "✗", ok ? "hub-check-icon hub-check-icon--ok" : "hub-check-icon hub-check-icon--fail");
            var label = Label(text, "hub-check-label");
            row.Add(icon);
            row.Add(label);
            return row;
        }

        public static VisualElement Field(string label, string value, Action<string> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-field-row");
            row.Add(Label(label, "hub-field-label"));

            var field = new TextField { value = value };
            field.AddToClassList("hub-field-input");
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            row.Add(field);
            return row;
        }

        public static VisualElement ImportModeSelector(Action refresh)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-import-mode");

            row.Add(Label("导入模式", "hub-field-label"));

            var btnRow = new VisualElement();
            btnRow.AddToClassList("hub-import-mode-row");

            var gitBtn = Button("Git UPM", "hub-tab", () =>
            {
                if (HubSettings.ImportMode == HubImportMode.GitUpm) return;
                HubSettings.ImportMode = HubImportMode.GitUpm;
                UpdateImportModeButtons(btnRow);
                refresh?.Invoke();
            });
            gitBtn.name = "hub-mode-git";

            var assetsBtn = Button("Assets 嵌入", "hub-tab", () =>
            {
                if (HubSettings.ImportMode == HubImportMode.AssetsEmbed) return;
                HubSettings.ImportMode = HubImportMode.AssetsEmbed;
                UpdateImportModeButtons(btnRow);
                refresh?.Invoke();
            });
            assetsBtn.name = "hub-mode-assets";

            btnRow.Add(gitBtn);
            btnRow.Add(assetsBtn);
            row.Add(btnRow);
            row.Add(Label(HubSettings.AssetsPackageRoot, "hub-sidebar-hint"));

            UpdateImportModeButtons(btnRow);
            return row;
        }

        private static void UpdateImportModeButtons(VisualElement btnRow)
        {
            var git = btnRow.Q<Button>("hub-mode-git");
            var assets = btnRow.Q<Button>("hub-mode-assets");
            if (git != null)
                git.EnableInClassList("hub-tab--active", HubSettings.ImportMode == HubImportMode.GitUpm);
            if (assets != null)
                assets.EnableInClassList("hub-tab--active", HubSettings.ImportMode == HubImportMode.AssetsEmbed);
        }

        public static VisualElement ToggleRow(string label, bool value, Action<bool> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-field-row");

            var toggle = new Toggle(label) { value = value };
            toggle.AddToClassList("hub-toggle");
            toggle.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            row.Add(toggle);
            return row;
        }
    }
}
#endif
