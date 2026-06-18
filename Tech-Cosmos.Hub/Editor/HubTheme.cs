#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    internal static class HubTheme
    {
        private const string StylesheetRelative = "Editor/UI/TechCosmosHub.uss";

        public static StyleSheet Load()
        {
            foreach (var path in EnumerateStylesheetAssetPaths())
            {
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null)
                    return sheet;
            }

            return null;
        }

        public static bool TryAttach(VisualElement root, out StyleSheet sheet)
        {
            sheet = Load();
            if (sheet == null || root == null)
                return false;

            if (!root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);
            return true;
        }

        public static IEnumerable<string> EnumerateStylesheetAssetPaths()
        {
            var seen = new HashSet<string>();

            foreach (var path in CollectPaths())
            {
                if (string.IsNullOrEmpty(path)) continue;
                path = path.Replace('\\', '/');
                if (seen.Add(path))
                    yield return path;
            }
        }

        private static IEnumerable<string> CollectPaths()
        {
            var seen = new HashSet<string>();

            string TryAdd(string path)
            {
                if (string.IsNullOrEmpty(path)) return null;
                path = path.Replace('\\', '/');
                return seen.Add(path) ? path : null;
            }

            var fromHub = TryAdd($"{HubPaths.HubAssetPath}/{StylesheetRelative}");
            if (fromHub != null) yield return fromHub;

            try
            {
                var info = PackageInfo.FindForPackageName(HubCatalog.SelfPackageId);
                if (info != null && !string.IsNullOrEmpty(info.assetPath))
                {
                    var fromPkg = TryAdd($"{info.assetPath}/{StylesheetRelative}");
                    if (fromPkg != null) yield return fromPkg;
                }
            }
            catch
            {
                // ignored
            }

            var guids = AssetDatabase.FindAssets("TechCosmosHub t:StyleSheet");
            foreach (var guid in guids)
            {
                var fromSearch = TryAdd(AssetDatabase.GUIDToAssetPath(guid));
                if (fromSearch != null) yield return fromSearch;
            }

            var disk = Path.Combine(HubPaths.HubRoot, StylesheetRelative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(disk))
            {
                var relative = ToProjectRelativeAssetPath(disk);
                if (!string.IsNullOrEmpty(relative))
                {
                    var fromDisk = TryAdd(relative);
                    if (fromDisk != null) yield return fromDisk;
                }
            }
        }

        private static string ToProjectRelativeAssetPath(string fullPath)
        {
            var dataPath = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar);
            var full = Path.GetFullPath(fullPath);
            if (!full.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
                return null;
            return full.Substring(dataPath.Length - "Assets".Length).Replace('\\', '/');
        }

        public static void ApplyShellLayout(VisualElement root, VisualElement body, VisualElement detailPanel)
        {
            if (root != null)
            {
                root.style.flexGrow = 1;
                root.style.flexShrink = 1;
                root.style.flexDirection = FlexDirection.Column;
                root.style.minHeight = 0;
            }

            if (body != null)
            {
                body.style.flexGrow = 1;
                body.style.flexShrink = 1;
                body.style.flexDirection = FlexDirection.Row;
                body.style.minHeight = 100;
            }

            if (detailPanel != null)
            {
                detailPanel.style.flexGrow = 1;
                detailPanel.style.flexShrink = 1;
                detailPanel.style.flexDirection = FlexDirection.Column;
                detailPanel.style.minWidth = 0;
                detailPanel.style.minHeight = 0;
                detailPanel.style.overflow = Overflow.Hidden;
            }
        }

        public static void ApplyDetailShellLayout(
            VisualElement top, ScrollView scroll, VisualElement scrollContent)
        {
            if (top != null)
            {
                top.style.flexShrink = 0;
                top.style.flexGrow = 0;
                top.style.flexDirection = FlexDirection.Column;
            }

            if (scroll != null)
            {
                scroll.style.flexGrow = 1;
                scroll.style.flexShrink = 1;
                scroll.style.minHeight = 60;
                scroll.contentContainer.style.flexDirection = FlexDirection.Column;
                scroll.contentContainer.style.alignItems = Align.Stretch;
            }

            if (scrollContent != null)
            {
                scrollContent.style.flexDirection = FlexDirection.Column;
                scrollContent.style.flexShrink = 0;
                scrollContent.style.alignItems = Align.Stretch;
            }
        }

        public static void ApplySidebarLayout(VisualElement sidebar)
        {
            if (sidebar == null) return;
            sidebar.style.flexShrink = 0;
            sidebar.style.flexGrow = 0;
            sidebar.style.flexDirection = FlexDirection.Column;
            sidebar.style.minHeight = 0;
        }

        public static void ApplyDependencySectionLayout(VisualElement section)
        {
            if (section == null) return;
            section.style.flexShrink = 0;
            section.style.flexDirection = FlexDirection.Column;
        }

        public static void ApplyReadmeCardLayout(VisualElement card)
        {
            if (card == null) return;
            card.style.flexShrink = 0;
            card.style.flexDirection = FlexDirection.Column;
            card.style.overflow = Overflow.Visible;
        }
    }
}
#endif
