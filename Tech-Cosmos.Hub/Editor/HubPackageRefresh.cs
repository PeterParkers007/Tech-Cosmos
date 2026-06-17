#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    [InitializeOnLoad]
    internal static class HubPackageRefresh
    {
        static HubPackageRefresh()
        {
            Events.registeredPackages += OnRegisteredPackages;
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            if (!TouchesHub(args))
                return;

            PackageReadmeLoader.ClearCache();
            EditorApplication.delayCall += () =>
            {
                TechCosmosHubWindow.RefreshAllOpenWindows();
                Debug.Log($"[Tech-Cosmos Hub] 包已变更，已刷新。Catalog: {HubPaths.CatalogJson}");
            };
        }

        private static bool TouchesHub(PackageRegistrationEventArgs args)
        {
            return ContainsHub(args.added)
                || ContainsHub(args.removed)
                || ContainsHub(args.changedFrom);
        }

        private static bool ContainsHub(System.Collections.Generic.IEnumerable<UnityEditor.PackageManager.PackageInfo> packages)
        {
            if (packages == null) return false;
            foreach (var p in packages)
            {
                if (p != null && p.name == HubCatalog.SelfPackageId)
                    return true;
            }
            return false;
        }
    }
}
#endif
