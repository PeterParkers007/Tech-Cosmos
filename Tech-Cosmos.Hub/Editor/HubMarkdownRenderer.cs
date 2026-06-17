#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace TechCosmos.Hub.Editor
{
    internal static class HubMarkdownRenderer
    {
        private static readonly Regex LinkInlineRegex = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex CodeInlineRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex BoldInlineRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
        private static readonly Regex ItalicInlineRegex = new(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.Compiled);

        private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex UlRegex = new(@"^[\-*+]\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex OlRegex = new(@"^\d+\.\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex HrRegex = new(@"^(\*{3,}|-{3,}|_{3,})\s*$", RegexOptions.Compiled);
        private static readonly Regex TableSepRegex = new(@"^\|?[\s:\-|]+\|?$", RegexOptions.Compiled);

        public static VisualElement Render(string markdown, string baseDirectory = null, int maxChars = 24000)
        {
            var root = new VisualElement();
            root.AddToClassList("hub-md");

            if (string.IsNullOrWhiteSpace(markdown))
            {
                root.Add(MakePlainLabel("（无内容）", "hub-md-empty"));
                return root;
            }

            var truncated = false;
            if (markdown.Length > maxChars)
            {
                markdown = markdown.Substring(0, maxChars);
                truncated = true;
            }

            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var blocks = ParseBlocks(lines);

            foreach (var block in blocks)
                root.Add(RenderBlock(block, baseDirectory));

            if (truncated)
                root.Add(MakePlainLabel("…（已截断，点击「打开 README」查看完整文件）", "hub-md-truncate"));

            return root;
        }

        private static List<MdBlock> ParseBlocks(string[] lines)
        {
            var blocks = new List<MdBlock>();
            var i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmed = line.TrimEnd();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    i++;
                    continue;
                }

                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    var codeLines = new List<string>();
                    i++;
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
                    {
                        codeLines.Add(lines[i]);
                        i++;
                    }

                    if (i < lines.Length) i++;
                    blocks.Add(new MdBlock(MdBlockKind.Code, codeLines));
                    continue;
                }

                var heading = HeadingRegex.Match(trimmed);
                if (heading.Success)
                {
                    blocks.Add(new MdBlock(MdBlockKind.Heading, heading.Groups[2].Value, heading.Groups[1].Value.Length));
                    i++;
                    continue;
                }

                if (HrRegex.IsMatch(trimmed))
                {
                    blocks.Add(new MdBlock(MdBlockKind.Hr));
                    i++;
                    continue;
                }

                if (trimmed.StartsWith(">", StringComparison.Ordinal))
                {
                    var quoteLines = new List<string>();
                    while (i < lines.Length)
                    {
                        var q = lines[i].TrimEnd();
                        if (string.IsNullOrWhiteSpace(q)) break;
                        if (!q.TrimStart().StartsWith(">", StringComparison.Ordinal)) break;
                        quoteLines.Add(q.TrimStart().TrimStart('>').TrimStart());
                        i++;
                    }

                    blocks.Add(new MdBlock(MdBlockKind.Quote, quoteLines));
                    continue;
                }

                if (trimmed.Contains('|') && i + 1 < lines.Length && TableSepRegex.IsMatch(lines[i + 1].Trim()))
                {
                    var tableLines = new List<string> { trimmed, lines[i + 1].Trim() };
                    i += 2;
                    while (i < lines.Length)
                    {
                        var row = lines[i].TrimEnd();
                        if (string.IsNullOrWhiteSpace(row) || !row.Contains('|')) break;
                        tableLines.Add(row.Trim());
                        i++;
                    }

                    blocks.Add(new MdBlock(MdBlockKind.Table, tableLines));
                    continue;
                }

                var ul = UlRegex.Match(trimmed);
                if (ul.Success)
                {
                    var items = new List<string>();
                    while (i < lines.Length)
                    {
                        var itemLine = lines[i].TrimEnd();
                        if (string.IsNullOrWhiteSpace(itemLine)) break;
                        var m = UlRegex.Match(itemLine.Trim());
                        if (!m.Success) break;
                        items.Add(m.Groups[1].Value);
                        i++;
                    }

                    blocks.Add(new MdBlock(MdBlockKind.UnorderedList, items));
                    continue;
                }

                var ol = OlRegex.Match(trimmed);
                if (ol.Success)
                {
                    var items = new List<string>();
                    while (i < lines.Length)
                    {
                        var itemLine = lines[i].TrimEnd();
                        if (string.IsNullOrWhiteSpace(itemLine)) break;
                        var m = OlRegex.Match(itemLine.Trim());
                        if (!m.Success) break;
                        items.Add(m.Groups[1].Value);
                        i++;
                    }

                    blocks.Add(new MdBlock(MdBlockKind.OrderedList, items));
                    continue;
                }

                var paraLines = new List<string> { trimmed };
                i++;
                while (i < lines.Length)
                {
                    var next = lines[i].TrimEnd();
                    if (string.IsNullOrWhiteSpace(next)) break;
                    if (IsBlockStarter(next)) break;
                    paraLines.Add(next.Trim());
                    i++;
                }

                blocks.Add(new MdBlock(MdBlockKind.Paragraph, string.Join(" ", paraLines)));
            }

            return blocks;
        }

        private static bool IsBlockStarter(string line)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal)) return true;
            if (HeadingRegex.IsMatch(trimmed)) return true;
            if (HrRegex.IsMatch(trimmed)) return true;
            if (trimmed.StartsWith(">", StringComparison.Ordinal)) return true;
            if (UlRegex.IsMatch(trimmed)) return true;
            if (OlRegex.IsMatch(trimmed)) return true;
            return trimmed.Contains('|');
        }

        private static VisualElement RenderBlock(MdBlock block, string baseDirectory)
        {
            switch (block.Kind)
            {
                case MdBlockKind.Heading:
                    return MakeRichLabel(block.Text, $"hub-md-h{Mathf.Clamp(block.Level, 1, 6)}", baseDirectory);
                case MdBlockKind.Paragraph:
                    return MakeRichLabel(block.Text, "hub-md-p", baseDirectory);
                case MdBlockKind.Code:
                    return RenderCodeBlock(block.Lines);
                case MdBlockKind.Quote:
                    return RenderQuote(block.Lines, baseDirectory);
                case MdBlockKind.UnorderedList:
                    return RenderList(block.Lines, ordered: false, baseDirectory);
                case MdBlockKind.OrderedList:
                    return RenderList(block.Lines, ordered: true, baseDirectory);
                case MdBlockKind.Hr:
                    return RenderHr();
                case MdBlockKind.Table:
                    return RenderTable(block.Lines, baseDirectory);
                default:
                    return new VisualElement();
            }
        }

        private static VisualElement RenderCodeBlock(IReadOnlyList<string> lines)
        {
            var pre = new VisualElement();
            pre.AddToClassList("hub-md-pre");
            pre.Add(MakePlainLabel(string.Join("\n", lines), "hub-md-code"));
            return pre;
        }

        private static VisualElement RenderQuote(IReadOnlyList<string> lines, string baseDirectory)
        {
            var quote = new VisualElement();
            quote.AddToClassList("hub-md-blockquote");

            foreach (var line in lines)
                quote.Add(MakeRichLabel(line, "hub-md-quote-line", baseDirectory));

            return quote;
        }

        private static VisualElement RenderList(IReadOnlyList<string> items, bool ordered, string baseDirectory)
        {
            var list = new VisualElement();
            list.AddToClassList(ordered ? "hub-md-ol" : "hub-md-ul");

            for (var i = 0; i < items.Count; i++)
            {
                var li = new VisualElement();
                li.AddToClassList("hub-md-li");

                li.Add(MakePlainLabel(ordered ? $"{i + 1}." : "•", "hub-md-li-marker"));

                var content = MakeRichLabel(items[i], "hub-md-li-text", baseDirectory);
                content.AddToClassList("hub-md-li-content");
                li.Add(content);

                list.Add(li);
            }

            return list;
        }

        private static VisualElement RenderHr()
        {
            var hr = new VisualElement();
            hr.AddToClassList("hub-md-hr");
            return hr;
        }

        private static VisualElement RenderTable(IReadOnlyList<string> lines, string baseDirectory)
        {
            var table = new VisualElement();
            table.AddToClassList("hub-md-table");

            if (lines.Count < 2) return table;

            var rows = new List<List<string>>();
            var headerCells = SplitTableRow(lines[0]);
            if (headerCells.Count > 0 && !IsTableSeparatorLine(lines[0]))
                rows.Add(headerCells);

            for (var r = 2; r < lines.Count; r++)
            {
                var line = lines[r].Trim();
                if (string.IsNullOrEmpty(line) || IsTableSeparatorLine(line)) continue;
                var cells = SplitTableRow(line);
                if (cells.Count > 0) rows.Add(cells);
            }

            if (rows.Count == 0) return table;

            var columnCount = 0;
            foreach (var row in rows)
                columnCount = Math.Max(columnCount, row.Count);
            if (columnCount == 0) return table;

            var colWidths = ComputeColumnMinWidths(rows, columnCount);
            var totalWidth = 0f;
            foreach (var w in colWidths) totalWidth += w;
            table.style.minWidth = totalWidth;

            var body = new VisualElement();
            body.AddToClassList("hub-md-table-body");
            body.style.minWidth = totalWidth;

            for (var r = 0; r < rows.Count; r++)
            {
                var isHeader = r == 0;
                var rowEl = new VisualElement();
                rowEl.AddToClassList(isHeader ? "hub-md-tr hub-md-tr--head" : "hub-md-tr");
                if (!isHeader && (r - 1) % 2 == 1)
                    rowEl.AddToClassList("hub-md-tr--alt");
                rowEl.style.minWidth = totalWidth;
                rowEl.style.flexDirection = FlexDirection.Row;
                rowEl.style.flexWrap = Wrap.NoWrap;
                rowEl.style.alignItems = Align.FlexStart;

                for (var c = 0; c < columnCount; c++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList(isHeader ? "hub-md-th" : "hub-md-td");
                    ApplyTableColumnWidth(cell, colWidths[c]);

                    var text = c < rows[r].Count ? rows[r][c].Trim() : string.Empty;
                    cell.Add(MakeTableCellLabel(text, isHeader, baseDirectory));
                    rowEl.Add(cell);
                }

                body.Add(rowEl);
            }

            table.Add(body);

            if (totalWidth > 400f)
            {
                var scroll = new ScrollView(ScrollViewMode.Horizontal);
                scroll.AddToClassList("hub-md-table-scroll");
                scroll.Add(table);
                return scroll;
            }

            return table;
        }

        private static void ApplyTableColumnWidth(VisualElement cell, float width)
        {
            cell.style.flexGrow = 0;
            cell.style.flexShrink = 0;
            cell.style.flexBasis = width;
            cell.style.width = width;
            cell.style.minWidth = width;
            cell.style.maxWidth = width;
            cell.style.overflow = Overflow.Visible;
            cell.style.flexDirection = FlexDirection.Column;
            cell.style.alignItems = Align.Stretch;
            cell.style.alignSelf = Align.FlexStart;
            cell.style.justifyContent = Justify.FlexStart;
        }

        private static bool IsTableSeparatorLine(string line)
            => !string.IsNullOrWhiteSpace(line) && TableSepRegex.IsMatch(line.Trim());

        private static Label MakeTableCellLabel(string text, bool isHeader, string baseDirectory)
        {
            Label label;
            if (isHeader || !ContainsInlineMarkdown(text))
            {
                label = MakePlainLabel(
                    text,
                    isHeader ? "hub-md-cell hub-md-cell--head" : "hub-md-cell");
            }
            else
            {
                label = MakeRichLabel(text, "hub-md-cell", baseDirectory);
            }

            ConfigureTableCellLabel(label);
            return label;
        }

        private static bool ContainsInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf('`') >= 0
                || text.IndexOf('*') >= 0
                || text.IndexOf('[') >= 0;
        }

        private static float[] ComputeColumnMinWidths(List<List<string>> rows, int columnCount)
        {
            const float charUnit = 7f;
            const float cellPadding = 22f;
            const float minCol = 64f;
            const float maxCol = 360f;

            var widths = new float[columnCount];
            for (var c = 0; c < columnCount; c++)
            {
                var maxUnits = 6;
                foreach (var row in rows)
                {
                    if (c >= row.Count) continue;
                    var plain = StripMarkdownForMeasure(row[c]);
                    maxUnits = Math.Max(maxUnits, EstimateDisplayUnits(plain));
                }

                widths[c] = Mathf.Clamp(maxUnits * charUnit + cellPadding, minCol, maxCol);
            }

            return widths;
        }

        private static string StripMarkdownForMeasure(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = LinkInlineRegex.Replace(text, "$1");
            text = CodeInlineRegex.Replace(text, "$1");
            text = BoldInlineRegex.Replace(text, "$1");
            text = ItalicInlineRegex.Replace(text, "$1");
            return text.Trim();
        }

        private static int EstimateDisplayUnits(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var units = 0;
            foreach (var ch in text)
                units += ch > 255 ? 2 : 1;
            return units;
        }

        private static void ConfigureTableCellLabel(Label label)
        {
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Visible;
            label.style.width = Length.Percent(100);
            label.style.flexGrow = 0;
            label.style.flexShrink = 0;
            label.style.marginTop = 0;
            label.style.marginBottom = 0;
            label.style.paddingTop = 0;
            label.style.paddingBottom = 0;
            label.style.minHeight = 0;
        }

        private static List<string> SplitTableRow(string line)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("|", StringComparison.Ordinal)) trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("|", StringComparison.Ordinal)) trimmed = trimmed.Substring(0, trimmed.Length - 1);

            var parts = trimmed.Split('|');
            var cells = new List<string>(parts.Length);
            foreach (var part in parts)
                cells.Add(part);
            return cells;
        }

        private static Label MakePlainLabel(string text, string className)
        {
            var label = new Label(text) { text = text, enableRichText = false };
            if (!string.IsNullOrEmpty(className)) label.AddToClassList(className);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label MakeRichLabel(string markdown, string className, string baseDirectory)
        {
            var links = new Dictionary<string, string>();
            var linkCounter = 0;
            var richText = ToRichText(markdown, links, ref linkCounter);

            var label = new Label { text = richText, enableRichText = true };
            if (!string.IsNullOrEmpty(className)) label.AddToClassList(className);
            label.style.whiteSpace = WhiteSpace.Normal;

            if (links.Count > 0)
            {
                label.RegisterCallback<PointerUpLinkTagEvent>(evt =>
                {
                    if (!string.IsNullOrEmpty(evt.linkID) && links.TryGetValue(evt.linkID, out var url))
                        OpenLink(url, baseDirectory);
                });
            }

            return label;
        }

        private static string ToRichText(string text, Dictionary<string, string> links, ref int linkCounter)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder();
            AppendRichInline(sb, text, links, ref linkCounter);
            return sb.ToString();
        }

        private static void AppendRichInline(StringBuilder sb, string text, Dictionary<string, string> links, ref int linkCounter)
        {
            if (string.IsNullOrEmpty(text)) return;

            var patterns = new (Regex Regex, MdInlineKind Kind)[]
            {
                (LinkInlineRegex, MdInlineKind.Link),
                (CodeInlineRegex, MdInlineKind.Code),
                (BoldInlineRegex, MdInlineKind.Bold),
                (ItalicInlineRegex, MdInlineKind.Italic),
            };

            var bestIndex = -1;
            var bestKind = MdInlineKind.None;
            Match bestMatch = null;

            foreach (var (regex, kind) in patterns)
            {
                var m = regex.Match(text);
                if (!m.Success) continue;
                if (bestIndex < 0 || m.Index < bestIndex)
                {
                    bestIndex = m.Index;
                    bestKind = kind;
                    bestMatch = m;
                }
            }

            if (bestMatch == null)
            {
                sb.Append(EscapeRichText(text));
                return;
            }

            if (bestIndex > 0)
                sb.Append(EscapeRichText(text.Substring(0, bestIndex)));

            switch (bestKind)
            {
                case MdInlineKind.Link:
                {
                    var label = bestMatch.Groups[1].Value;
                    var url = bestMatch.Groups[2].Value;
                    var linkId = $"hub-link-{linkCounter++}";
                    links[linkId] = url;
                    sb.Append("<color=#60A5FA><u><link=\"");
                    sb.Append(linkId);
                    sb.Append("\">");
                    sb.Append(EscapeRichText(label));
                    sb.Append("</link></u></color>");
                    break;
                }
                case MdInlineKind.Code:
                    sb.Append("<color=#E6B478><size=12>");
                    sb.Append(EscapeRichText(bestMatch.Groups[1].Value));
                    sb.Append("</size></color>");
                    break;
                case MdInlineKind.Bold:
                    sb.Append("<b>");
                    AppendRichInline(sb, bestMatch.Groups[1].Value, links, ref linkCounter);
                    sb.Append("</b>");
                    break;
                case MdInlineKind.Italic:
                    sb.Append("<i>");
                    AppendRichInline(sb, bestMatch.Groups[1].Value, links, ref linkCounter);
                    sb.Append("</i>");
                    break;
            }

            var rest = text.Substring(bestIndex + bestMatch.Length);
            AppendRichInline(sb, rest, links, ref linkCounter);
        }

        private static string EscapeRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static void OpenLink(string url, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            url = url.Trim();

            var hashIndex = url.IndexOf('#');
            if (hashIndex >= 0)
                url = url.Substring(0, hashIndex);

            if (url.Length == 0) return;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Application.OpenURL(url);
                return;
            }

            if (url.StartsWith("#", StringComparison.Ordinal)) return;

            try
            {
                var normalized = url.Replace('/', Path.DirectorySeparatorChar);
                string fullPath;

                if (!Path.IsPathRooted(normalized) && !string.IsNullOrEmpty(baseDirectory))
                    fullPath = Path.GetFullPath(Path.Combine(baseDirectory, normalized));
                else
                    fullPath = Path.GetFullPath(normalized);

                if (File.Exists(fullPath))
                {
                    EditorUtility.OpenWithDefaultApp(fullPath);
                    return;
                }

                if (Directory.Exists(fullPath))
                {
                    EditorUtility.RevealInFinder(fullPath);
                    return;
                }

                var assetPath = ToAssetPath(fullPath);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset != null)
                    {
                        AssetDatabase.OpenAsset(asset);
                        EditorGUIUtility.PingObject(asset);
                        return;
                    }
                }

                Debug.LogWarning($"[Tech-Cosmos Hub] 无法打开链接: {url}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Tech-Cosmos Hub] 链接打开失败: {url} — {ex.Message}");
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(fullPath);
            if (!full.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)) return null;
            return full.Substring(projectRoot.Length + 1).Replace('\\', '/');
        }

        private enum MdBlockKind
        {
            Paragraph,
            Heading,
            Code,
            Quote,
            UnorderedList,
            OrderedList,
            Hr,
            Table
        }

        private enum MdInlineKind
        {
            None,
            Link,
            Code,
            Bold,
            Italic
        }

        private sealed class MdBlock
        {
            public MdBlockKind Kind { get; }
            public string Text { get; }
            public int Level { get; }
            public IReadOnlyList<string> Lines { get; }

            public MdBlock(MdBlockKind kind, string text = null, int level = 0)
            {
                Kind = kind;
                Text = text;
                Level = level;
            }

            public MdBlock(MdBlockKind kind, IReadOnlyList<string> lines)
            {
                Kind = kind;
                Lines = lines;
            }
        }
    }
}
#endif
