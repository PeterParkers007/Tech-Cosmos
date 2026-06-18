#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace TechCosmos.Hub.Editor
{
    public enum PackageDependencyState
    {
        Installed,
        PendingImport,
        Blocked,
        Unknown
    }

    public sealed class PackageDependencyLink
    {
        public string PackageId;
        public string DisplayName;
        public PackageDependencyState State;
        public string Detail;
    }

    public sealed class PackageImportPlan
    {
        public PackageCatalogEntry Target;
        public bool CanImportSelf;
        public bool CanImport;
        public string BlockReason;
        public List<PackageCatalogEntry> ImportOrder = new();
        public List<PackageDependencyLink> DependsOn = new();
        public List<PackageDependencyLink> DependedBy = new();

        public int PendingDependencyCount =>
            DependsOn.Count(d => d.State == PackageDependencyState.PendingImport);
    }

    public static class PackageDependencyResolver
    {
        public static PackageCatalogEntry FindEntry(PackageCatalogFile catalog, string packageId)
        {
            if (catalog?.packages == null || string.IsNullOrEmpty(packageId)) return null;
            foreach (var p in catalog.packages)
            {
                if (p != null && p.id == packageId)
                    return p;
            }
            return null;
        }

        public static IEnumerable<string> GetDependsOnIds(PackageCatalogEntry entry)
        {
            if (entry?.dependsOn == null) yield break;
            foreach (var id in entry.dependsOn)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    yield return id.Trim();
            }
        }

        public static List<PackageCatalogEntry> GetDependents(PackageCatalogFile catalog, string packageId)
        {
            var list = new List<PackageCatalogEntry>();
            if (catalog?.packages == null || string.IsNullOrEmpty(packageId)) return list;

            foreach (var p in catalog.packages)
            {
                if (p == null || p.id == packageId) continue;
                foreach (var dep in GetDependsOnIds(p))
                {
                    if (dep == packageId)
                    {
                        list.Add(p);
                        break;
                    }
                }
            }

            return list;
        }

        public static PackageImportPlan Evaluate(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            var plan = new PackageImportPlan { Target = entry };
            if (entry == null || catalog == null)
            {
                plan.BlockReason = "无效的包或 catalog。";
                return plan;
            }

            plan.CanImportSelf = PackageDetector.CanImportSelf(entry, catalog);
            plan.DependsOn = BuildDependsOnLinks(entry, catalog);
            plan.DependedBy = BuildDependedByLinks(entry, catalog);

            foreach (var link in plan.DependsOn)
            {
                if (link.State == PackageDependencyState.Unknown)
                {
                    plan.CanImport = false;
                    plan.BlockReason = $"缺少依赖 {link.PackageId}（未收录于 catalog，请在 Hub Studio 补充或手动安装）。";
                    return plan;
                }

                if (link.State == PackageDependencyState.Blocked)
                {
                    plan.CanImport = false;
                    plan.BlockReason =
                        $"缺少依赖 {link.DisplayName}（{link.PackageId}），且该依赖当前无法导入（无 gitUrl / 本地源）。";
                    return plan;
                }
            }

            if (!plan.CanImportSelf)
            {
                plan.CanImport = false;
                if (PackageDetector.IsRequirementMet(entry.id, catalog))
                    plan.BlockReason = "该包已安装。";
                else
                    plan.BlockReason = "该包无法导入（无 gitUrl / 本地源，或已安装）。";
                return plan;
            }

            try
            {
                plan.ImportOrder = BuildImportOrder(entry, catalog);
            }
            catch (InvalidOperationException ex)
            {
                plan.CanImport = false;
                plan.BlockReason = ex.Message;
                return plan;
            }

            plan.CanImport = true;
            return plan;
        }

        private static List<PackageDependencyLink> BuildDependsOnLinks(
            PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            var links = new List<PackageDependencyLink>();
            foreach (var depId in GetDependsOnIds(entry))
            {
                var dep = FindEntry(catalog, depId);
                if (dep == null)
                {
                    links.Add(new PackageDependencyLink
                    {
                        PackageId = depId,
                        DisplayName = depId,
                        State = PackageDependencyState.Unknown,
                        Detail = "未收录"
                    });
                    continue;
                }

                links.Add(BuildLink(dep, catalog, isUpstream: true));
            }

            return links;
        }

        private static List<PackageDependencyLink> BuildDependedByLinks(
            PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            var links = new List<PackageDependencyLink>();
            foreach (var dependent in GetDependents(catalog, entry.id))
                links.Add(BuildLink(dependent, catalog, isUpstream: false));
            return links.OrderBy(l => l.DisplayName).ToList();
        }

        private static PackageDependencyLink BuildLink(
            PackageCatalogEntry pkg, PackageCatalogFile catalog, bool isUpstream)
        {
            var link = new PackageDependencyLink
            {
                PackageId = pkg.id,
                DisplayName = pkg.displayName ?? pkg.id
            };

            if (PackageDetector.IsRequirementMet(pkg.id, catalog))
            {
                link.State = PackageDependencyState.Installed;
                link.Detail = "已导入";
                return link;
            }

            if (PackageDetector.CanImportSelf(pkg, catalog))
            {
                link.State = PackageDependencyState.PendingImport;
                link.Detail = isUpstream ? "导入时将自动安装" : "依赖本包，尚未导入";
                return link;
            }

            link.State = PackageDependencyState.Blocked;
            link.Detail = "无法导入";
            return link;
        }

        private static List<PackageCatalogEntry> BuildImportOrder(
            PackageCatalogEntry target, PackageCatalogFile catalog)
        {
            var order = new List<PackageCatalogEntry>();
            var visiting = new HashSet<string>();
            var added = new HashSet<string>();

            void Visit(PackageCatalogEntry node)
            {
                if (node == null) return;
                if (added.Contains(node.id)) return;
                if (visiting.Contains(node.id))
                    throw new InvalidOperationException($"检测到循环依赖：{node.displayName ?? node.id}");

                visiting.Add(node.id);

                foreach (var depId in GetDependsOnIds(node))
                {
                    var dep = FindEntry(catalog, depId);
                    if (dep == null)
                        throw new InvalidOperationException($"依赖 {depId} 未收录于 catalog。");
                    if (!PackageDetector.IsRequirementMet(depId, catalog))
                        Visit(dep);
                }

                visiting.Remove(node.id);

                if (PackageDetector.IsRequirementMet(node.id, catalog))
                    return;

                if (!PackageDetector.CanImportSelf(node, catalog))
                    throw new InvalidOperationException(
                        $"无法导入 {node.displayName ?? node.id}：缺少可解析的安装源。");

                if (added.Add(node.id))
                    order.Add(node);
            }

            Visit(target);
            return order;
        }

        public static List<PackageCatalogEntry> BuildBatchImportOrder(
            IEnumerable<PackageCatalogEntry> targets, PackageCatalogFile catalog)
        {
            var merged = new List<PackageCatalogEntry>();
            var seen = new HashSet<string>();

            if (targets == null) return merged;

            foreach (var target in targets)
            {
                if (target == null) continue;
                var plan = Evaluate(target, catalog);
                if (!plan.CanImport) continue;

                foreach (var pkg in plan.ImportOrder)
                {
                    if (seen.Add(pkg.id))
                        merged.Add(pkg);
                }
            }

            return merged;
        }
    }
}
#endif
