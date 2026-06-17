#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    [InitializeOnLoad]
    internal static class HubEditorBootstrap
    {
        static HubEditorBootstrap()
        {
#if !UNITY_2022_2_OR_NEWER
            Debug.LogWarning(
                "[Tech-Cosmos Hub] 当前 Unity 版本低于 2022.2，Hub Editor 可能无法编译，顶部菜单不会出现。请使用 Unity 2022.3 LTS。");
#else
            Debug.Log("[Tech-Cosmos Hub] Editor 已加载。菜单入口：Tech-Cosmos → Hub");
#endif
        }

        [MenuItem("Tech-Cosmos/诊断 Hub 安装", false, 100)]
        private static void Diagnose()
        {
            var msg =
                "Hub Editor 程序集已加载。\n\n" +
                $"Unity: {Application.unityVersion}\n" +
                $"Hub 路径: {HubPaths.HubRoot}\n" +
                $"Catalog: {(System.IO.File.Exists(HubPaths.CatalogJson) ? "OK" : "缺失")}\n\n" +
                "若仍看不到 Tech-Cosmos 菜单，请查看 Console 红色编译错误。";

            EditorUtility.DisplayDialog("Tech-Cosmos Hub", msg, "确定");
        }
    }
}
#endif
