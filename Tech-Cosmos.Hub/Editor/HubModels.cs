using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    [Serializable]
    public class PackageCatalogFile
    {
        public string frameworkRoot = "Assets/Framework";
        public PackageCatalogEntry[] packages = Array.Empty<PackageCatalogEntry>();
    }

    [Serializable]
    public class PackageCatalogEntry
    {
        public string id;
        public string folder;
        public string displayName;
        public string category;
        public string description;
        public string gitUrl;
    }

    [Serializable]
    public class GlueRecipeFile
    {
        public string outputRoot = "Assets/_Game/Generated/Hub";
        public GlueRecipeEntry[] recipes = Array.Empty<GlueRecipeEntry>();
    }

    [Serializable]
    public class GlueRecipeEntry
    {
        public string id;
        public string displayName;
        public string description;
        public string[] requires = Array.Empty<string>();
        public string[] dependsOnRecipes = Array.Empty<string>();
        public string template;
        public bool needsHeroType;
    }

    [Serializable]
    public class ProjectStructureFile
    {
        public string defaultRoot = "Assets/_Game";
        public ProjectStructurePreset[] presets = Array.Empty<ProjectStructurePreset>();
    }

    [Serializable]
    public class ProjectStructurePreset
    {
        public string id;
        public string displayName;
        public string description;
        public string root = "Assets/_Game";
        public string[] folders = Array.Empty<string>();
        public ProjectStructureExtraRoot[] extraRoots = Array.Empty<ProjectStructureExtraRoot>();
    }

    [Serializable]
    public class ProjectStructureExtraRoot
    {
        public string root;
        public string[] folders = Array.Empty<string>();
    }

    public static class HubPaths
    {
        private static string _hubRoot;
        private static string _hubAssetPath;

        /// <summary>包在磁盘上的绝对路径（UPM Git / 本地嵌入均可用）。</summary>
        public static string HubRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(_hubRoot)) return _hubRoot;

                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HubPaths).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.resolvedPath))
                {
                    _hubRoot = info.resolvedPath;
                    return _hubRoot;
                }

                var embedded = Path.Combine(Application.dataPath, "Framework", "Tech-Cosmos.Hub");
                _hubRoot = embedded;
                return _hubRoot;
            }
        }

        /// <summary>包在 Unity 工程内的资源路径，如 Packages/com.techcosmos.hub。</summary>
        public static string HubAssetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_hubAssetPath)) return _hubAssetPath;

                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HubPaths).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.assetPath))
                {
                    _hubAssetPath = info.assetPath.Replace('\\', '/');
                    return _hubAssetPath;
                }

                _hubAssetPath = "Assets/Framework/Tech-Cosmos.Hub";
                return _hubAssetPath;
            }
        }

        public static string CatalogJson => Path.Combine(HubRoot, "Data", "package-catalog.json");
        public static string RecipesJson => Path.Combine(HubRoot, "Data", "glue-recipes.json");
        public static string ProjectStructureJson => Path.Combine(HubRoot, "Data", "project-structure.json");
        public static string TemplatesDir => Path.Combine(HubRoot, "Data", "Templates");
        public static string ManifestPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    public static class HubSettings
    {
        private const string PrefsHeroType = "TechCosmos.Hub.HeroType";
        private const string PrefsHeroNamespace = "TechCosmos.Hub.HeroNamespace";
        private const string PrefsProjectRoot = "TechCosmos.Hub.ProjectRoot";
        private const string PrefsProjectName = "TechCosmos.Hub.ProjectName";
        private const string PrefsWriteGitKeep = "TechCosmos.Hub.WriteGitKeep";
        private const string PrefsWriteReadme = "TechCosmos.Hub.WriteReadme";
        private const string PrefsWriteAsmdef = "TechCosmos.Hub.WriteAsmdef";
        private const string PrefsSelectedStructurePreset = "TechCosmos.Hub.StructurePreset";

        public static string HeroType
        {
            get => EditorPrefs.GetString(PrefsHeroType, "IntegrationHero");
            set => EditorPrefs.SetString(PrefsHeroType, value);
        }

        public static string HeroNamespace
        {
            get => EditorPrefs.GetString(PrefsHeroNamespace, "TechCosmos.Game.Integration");
            set => EditorPrefs.SetString(PrefsHeroNamespace, value);
        }

        public static string ProjectRoot
        {
            get => EditorPrefs.GetString(PrefsProjectRoot, "Assets/_Game");
            set => EditorPrefs.SetString(PrefsProjectRoot, value);
        }

        public static string ProjectDisplayName
        {
            get => EditorPrefs.GetString(PrefsProjectName, "My Game");
            set => EditorPrefs.SetString(PrefsProjectName, value);
        }

        public static bool WriteGitKeep
        {
            get => EditorPrefs.GetBool(PrefsWriteGitKeep, true);
            set => EditorPrefs.SetBool(PrefsWriteGitKeep, value);
        }

        public static bool WriteReadme
        {
            get => EditorPrefs.GetBool(PrefsWriteReadme, true);
            set => EditorPrefs.SetBool(PrefsWriteReadme, value);
        }

        public static bool WriteAsmdefStubs
        {
            get => EditorPrefs.GetBool(PrefsWriteAsmdef, false);
            set => EditorPrefs.SetBool(PrefsWriteAsmdef, value);
        }

        public static string SelectedStructurePresetId
        {
            get => EditorPrefs.GetString(PrefsSelectedStructurePreset, "standard");
            set => EditorPrefs.SetString(PrefsSelectedStructurePreset, value);
        }
    }
}
