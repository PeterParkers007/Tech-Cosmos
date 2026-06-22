using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    public static class GlueGenerator
    {
        public static string GetOutputPattern(GlueRecipeEntry recipe, GlueTemplateMetaFile meta = null)
        {
            if (recipe == null) return null;
            if (!string.IsNullOrWhiteSpace(recipe.outputFile))
                return recipe.outputFile.Trim();

            meta ??= HubDataLoader.LoadTemplatesMeta();
            var templateEntry = meta?.FindEntry(recipe.template);
            if (!string.IsNullOrWhiteSpace(templateEntry?.outputFile))
                return templateEntry.outputFile.Trim();

            return string.IsNullOrWhiteSpace(recipe.template) ? null : recipe.template + ".g.cs";
        }

        public static string GetOutputFileName(GlueRecipeEntry recipe, GlueTemplateMetaFile meta = null)
        {
            var pattern = GetOutputPattern(recipe, meta);
            return string.IsNullOrWhiteSpace(pattern) ? null : ApplyPlaceholders(pattern, recipe);
        }

        public static string GetOutputFileName(string template)
        {
            if (string.IsNullOrWhiteSpace(template)) return null;
            var meta = HubDataLoader.LoadTemplatesMeta();
            var entry = meta.FindEntry(template);
            if (!string.IsNullOrWhiteSpace(entry?.outputFile))
                return ApplyPlaceholders(entry.outputFile, new GlueRecipeEntry { template = template, id = template });

            return template + ".g.cs";
        }

        public static GlueRecipeStatus Evaluate(
            GlueRecipeEntry recipe,
            PackageCatalogFile catalog,
            GlueRecipeFile recipeFile,
            GlueTemplateMetaFile meta = null)
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
                    if (!IsRecipeOutputPresent(dep, recipeFile, meta))
                        status.MissingRecipeOutputs.Add(dep);
                }
            }

            status.CanGenerate = status.MissingPackages.Count == 0 && status.MissingRecipeOutputs.Count == 0;
            return status;
        }

        public static bool Generate(GlueRecipeEntry recipe, GlueRecipeFile recipeFile, GlueTemplateMetaFile meta = null)
        {
            meta ??= HubDataLoader.LoadTemplatesMeta();
            var fileName = GetOutputFileName(recipe, meta);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException($"无法确定输出文件名: {recipe?.id}");

            if (string.IsNullOrWhiteSpace(recipe.template))
                throw new InvalidOperationException($"Recipe 缺少 template: {recipe.id}");

            var templatePath = Path.Combine(HubPaths.TemplatesDir, recipe.template + ".txt");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("模板文件不存在", templatePath);

            var content = ApplyPlaceholders(File.ReadAllText(templatePath), recipe);

            var outputRoot = string.IsNullOrWhiteSpace(recipeFile.outputRoot)
                ? "Assets/_Game/Generated/Hub"
                : recipeFile.outputRoot;
            var outputDir = Path.Combine(HubPaths.ProjectRoot, outputRoot);
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(outputDir, fileName);
            var outputFolder = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputFolder))
                Directory.CreateDirectory(outputFolder);

            File.WriteAllText(outputPath, content, Encoding.UTF8);

            AssetDatabase.Refresh();
            Debug.Log($"[Tech-Cosmos Hub] 已生成胶水: {outputRoot}/{fileName}");
            return true;
        }

        public static void GenerateAllReady(
            PackageCatalogFile catalog,
            GlueRecipeFile recipeFile,
            GlueTemplateMetaFile meta = null)
        {
            if (recipeFile?.recipes == null) return;
            meta ??= HubDataLoader.LoadTemplatesMeta();

            var generated = new HashSet<string>();
            for (var pass = 0; pass < recipeFile.recipes.Length + 1; pass++)
            {
                var any = false;
                foreach (var recipe in recipeFile.recipes)
                {
                    if (recipe == null || generated.Contains(recipe.id)) continue;

                    var status = Evaluate(recipe, catalog, recipeFile, meta);
                    if (!status.CanGenerate) continue;

                    Generate(recipe, recipeFile, meta);
                    generated.Add(recipe.id);
                    any = true;
                }

                if (!any) break;
            }
        }

        private static GlueRecipeEntry FindRecipe(GlueRecipeFile file, string id)
        {
            if (file.recipes == null) return null;
            foreach (var r in file.recipes)
                if (r.id == id) return r;
            return null;
        }

        private static bool IsRecipeOutputPresent(
            string recipeId,
            GlueRecipeFile recipeFile,
            GlueTemplateMetaFile meta = null)
        {
            var recipe = FindRecipe(recipeFile, recipeId);
            if (recipe == null) return false;

            var fileName = GetOutputFileName(recipe, meta);
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            var path = Path.Combine(HubPaths.ProjectRoot, recipeFile.outputRoot, fileName);
            return File.Exists(path);
        }

        private static string ApplyPlaceholders(string text, GlueRecipeEntry recipe)
        {
            if (string.IsNullOrEmpty(text) || recipe == null) return text;

            var displayName = string.IsNullOrWhiteSpace(recipe.displayName) ? recipe.id : recipe.displayName;
            return text
                .Replace("{{TEMPLATE}}", recipe.template ?? string.Empty)
                .Replace("{{RECIPE_ID}}", recipe.id ?? string.Empty)
                .Replace("{{DISPLAY_NAME}}", displayName ?? string.Empty)
                .Replace("{{HERO_TYPE}}", HubSettings.HeroType)
                .Replace("{{HERO_NAMESPACE}}", HubSettings.HeroNamespace)
                .Replace("{{PROJECT_NAME}}", HubSettings.ProjectDisplayName);
        }
    }

    public sealed class GlueRecipeStatus
    {
        public GlueRecipeEntry Recipe;
        public readonly System.Collections.Generic.List<string> MissingPackages = new();
        public readonly System.Collections.Generic.List<string> MissingRecipeOutputs = new();
        public bool CanGenerate;
    }
}
