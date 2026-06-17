using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    public static class GlueGenerator
    {
        private static readonly Dictionary<string, string> TemplateFileMap = new()
        {
            { "IntegrationEvents", "IntegrationEvents.g.cs" },
            { "GameEntityRegistry", "GameEntityRegistry.g.cs" },
            { "GameCompositionRoot", "GameCompositionRoot.g.cs" },
            { "CastFlowAdapter", "CastFlowAdapter.g.cs" },
            { "GameEntityBridge", "GameEntityBridge.g.cs" },
            { "ControllerInputSource", "ControllerInputSource.g.cs" }
        };

        public static string GetOutputFileName(string template)
        {
            return TemplateFileMap.TryGetValue(template, out var fileName) ? fileName : template + ".g.cs";
        }

        public static GlueRecipeStatus Evaluate(GlueRecipeEntry recipe, PackageCatalogFile catalog, GlueRecipeFile recipeFile)
        {
            var status = new GlueRecipeStatus { Recipe = recipe };

            if (recipe.requires != null)
            {
                foreach (var req in recipe.requires)
                {
                    if (!PackageDetector.IsRequirementMet(req, catalog))
                        status.MissingPackages.Add(req);
                }
            }

            if (recipe.dependsOnRecipes != null)
            {
                foreach (var dep in recipe.dependsOnRecipes)
                {
                    if (!IsRecipeOutputPresent(dep, recipeFile))
                        status.MissingRecipeOutputs.Add(dep);
                }
            }

            status.CanGenerate = status.MissingPackages.Count == 0 && status.MissingRecipeOutputs.Count == 0;
            return status;
        }

        public static bool Generate(GlueRecipeEntry recipe, GlueRecipeFile recipeFile)
        {
            if (!TemplateFileMap.TryGetValue(recipe.template, out var fileName))
                throw new InvalidOperationException($"未知模板: {recipe.template}");

            var templatePath = Path.Combine(HubPaths.TemplatesDir, recipe.template + ".txt");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("模板文件不存在", templatePath);

            var template = File.ReadAllText(templatePath);
            template = template.Replace("{{HERO_TYPE}}", HubSettings.HeroType);
            template = template.Replace("{{HERO_NAMESPACE}}", HubSettings.HeroNamespace);

            var outputDir = Path.Combine(HubPaths.ProjectRoot, recipeFile.outputRoot);
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(outputDir, fileName);
            File.WriteAllText(outputPath, template, Encoding.UTF8);

            AssetDatabase.Refresh();
            Debug.Log($"[Tech-Cosmos Hub] 已生成胶水: {recipeFile.outputRoot}/{fileName}");
            return true;
        }

        public static void GenerateAllReady(PackageCatalogFile catalog, GlueRecipeFile recipeFile)
        {
            var order = new[]
            {
                "integration-events",
                "entity-registry",
                "composition-root",
                "cast-flow",
                "entity-bridge",
                "controller-input"
            };

            foreach (var id in order)
            {
                var recipe = FindRecipe(recipeFile, id);
                if (recipe == null) continue;

                var status = Evaluate(recipe, catalog, recipeFile);
                if (!status.CanGenerate) continue;

                Generate(recipe, recipeFile);
            }
        }

        private static GlueRecipeEntry FindRecipe(GlueRecipeFile file, string id)
        {
            if (file.recipes == null) return null;
            foreach (var r in file.recipes)
                if (r.id == id) return r;
            return null;
        }

        private static bool IsRecipeOutputPresent(string recipeId, GlueRecipeFile recipeFile)
        {
            var recipe = FindRecipe(recipeFile, recipeId);
            if (recipe == null || !TemplateFileMap.TryGetValue(recipe.template, out var fileName))
                return false;

            var path = Path.Combine(HubPaths.ProjectRoot, recipeFile.outputRoot, fileName);
            return File.Exists(path);
        }
    }

    public sealed class GlueRecipeStatus
    {
        public GlueRecipeEntry Recipe;
        public readonly List<string> MissingPackages = new();
        public readonly List<string> MissingRecipeOutputs = new();
        public bool CanGenerate;
    }
}
