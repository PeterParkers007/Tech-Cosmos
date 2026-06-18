#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace TechCosmos.Hub.Editor
{
    internal static class HubTheme
    {
        private const string StylesheetRelative = "Editor/UI/TechCosmosHub.uss";

        public static StyleSheet Load()
        {
            foreach (var path in EnumerateStylesheetAssetPaths())
            {
                var sheet = LoadAtPath(path);
                if (sheet != null)
                    return sheet;
            }

            return null;
        }

        public static bool Attach(VisualElement root, ref StyleSheet current, bool forceReload)
        {
            if (root == null)
                return false;

            if (forceReload && current != null)
            {
                root.styleSheets.Remove(current);
                current = null;
            }

            if (current != null && root.styleSheets.Contains(current))
                return true;

            if (current != null)
                root.styleSheets.Remove(current);

            current = Load();
            if (current == null)
                return false;

            if (!root.styleSheets.Contains(current))
                root.styleSheets.Add(current);
            return true;
        }

        private static StyleSheet LoadAtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            assetPath = assetPath.Replace('\\', '/');
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            if (sheet != null)
                return sheet;

            if (assetPath.StartsWith("Packages/", System.StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/", System.StringComparison.Ordinal))
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            }

            return sheet;
        }

        public static IEnumerable<string> EnumerateStylesheetAssetPaths()
        {
            var seen = new HashSet<string>();
            foreach (var path in CollectPaths())
            {
                if (string.IsNullOrEmpty(path)) continue;
                var normalized = path.Replace('\\', '/');
                if (seen.Add(normalized))
                    yield return normalized;
            }
        }

        private static IEnumerable<string> CollectPaths()
        {
            var results = new List<string>();
            var seen = new HashSet<string>();

            void Add(string path)
            {
                if (string.IsNullOrEmpty(path)) return;
                path = path.Replace('\\', '/');
                if (seen.Add(path))
                    results.Add(path);
            }

            try
            {
                var info = UpmPackageInfo.FindForPackageName(HubCatalog.SelfPackageId);
                if (info != null && !string.IsNullOrEmpty(info.assetPath))
                    Add($"{info.assetPath}/{StylesheetRelative}");
            }
            catch
            {
                // ignored
            }

            Add($"{HubPaths.HubAssetPath}/{StylesheetRelative}");

            var guids = AssetDatabase.FindAssets("TechCosmosHub t:StyleSheet");
            foreach (var guid in guids)
                Add(AssetDatabase.GUIDToAssetPath(guid));

            return results;
        }

        public static void ApplyFallbackChrome(VisualElement root, VisualElement header, VisualElement body)
        {
            var pageBg = new Color(22f / 255f, 24f / 255f, 30f / 255f);
            var panelBg = new Color(30f / 255f, 33f / 255f, 42f / 255f);
            var headerBg = new Color(28f / 255f, 32f / 255f, 42f / 255f);

            if (root != null)
            {
                root.style.backgroundColor = pageBg;
                root.style.flexDirection = FlexDirection.Column;
                root.style.flexGrow = 1;
            }

            if (header != null)
                header.style.backgroundColor = headerBg;

            if (body != null)
            {
                body.style.flexDirection = FlexDirection.Row;
                body.style.flexGrow = 1;
            }

            ApplyShellLayout(root, body, null);
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
                detailPanel.style.backgroundColor = new Color(30f / 255f, 33f / 255f, 42f / 255f);
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
            sidebar.style.backgroundColor = new Color(30f / 255f, 33f / 255f, 42f / 255f);
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
