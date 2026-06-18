#if UNITY_EDITOR
using UnityEditor;

namespace TechCosmos.Hub.Editor
{
    [InitializeOnLoad]
    internal static class HubStyleBootstrap
    {
        static HubStyleBootstrap()
        {
            EditorApplication.delayCall += EnsureStylesImported;
        }

        private static void EnsureStylesImported()
        {
            foreach (var path in HubTheme.EnumerateStylesheetAssetPaths())
            {
                if (string.IsNullOrEmpty(path)) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            var uxml = $"{HubPaths.HubAssetPath}/Editor/UI/TechCosmosHub.uxml".Replace('\\', '/');
            if (!string.IsNullOrEmpty(uxml))
                AssetDatabase.ImportAsset(uxml, ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif
