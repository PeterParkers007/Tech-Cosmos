#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    internal static class HubMarkdownRenderer
    {
        private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex UlRegex = new(@"^[\-*+]\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex OlRegex = new(@"^\d+\.\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex HrRegex = new(@"^(\*{3,}|-{3,}|_{3,})\s*$", RegexOptions.Compiled);
        private static readonly Regex TableSepRegex = new(@"^\|?[\s:\-|]+\|?$", RegexOptions.Compiled);

        private static readonly Regex MeasureLinkedImageRegex =
            new(@"\[!\[([^\]]*)\]\(([^)]+)\)\]\(([^)]+)\)", RegexOptions.Compiled);

        private static readonly Regex MeasureImageRegex =
            new(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);

        private static readonly Regex MeasureLinkRegex =
            new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);

        private static readonly Regex MeasureCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex MeasureBoldRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
        private static readonly Regex MeasureItalicRegex = new(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.Compiled);

        internal static string NormalizeLineEndings(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        public static VisualElement Render(string markdown, string baseDirectory = null, int maxChars = 24000)
        {
            var root = new VisualElement();
            root.AddToClassList("hub-md");
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexShrink = 0;
            root.style.alignItems = Align.Stretch;

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

            markdown = NormalizeLineEndings(markdown);
            var lines = markdown.Split('\n');
            var blocks = ParseBlocks(lines);

            foreach (var block in blocks)
                root.Add(WrapBlock(RenderBlock(block, baseDirectory)));

            if (truncated)
                root.Add(MakePlainLabel("…（已截断，点击「打开 README」查看完整文件）", "hub-md-truncate"));

            HubMarkdownFonts.ApplyEmojiTree(root);
            HubMarkdownLayout.ApplyTree(root);
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
                    blocks.Add(ParseListBlock(lines, ref i, ordered: false));
                    continue;
                }

                var ol = OlRegex.Match(trimmed);
                if (ol.Success)
                {
                    blocks.Add(ParseListBlock(lines, ref i, ordered: true));
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

        private static MdBlock ParseListBlock(string[] lines, ref int i, bool ordered)
        {
            var regex = ordered ? OlRegex : UlRegex;
            var items = new List<MdListItem>();

            while (i < lines.Length)
            {
                var itemLine = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(itemLine)) break;

                var match = regex.Match(itemLine.Trim());
                if (!match.Success) break;

                i++;
                var children = ParseIndentedChildren(lines, ref i);
                items.Add(new MdListItem(match.Groups[1].Value, children));
            }

            return new MdBlock(ordered ? MdBlockKind.OrderedList : MdBlockKind.UnorderedList, items);
        }

        private static List<MdBlock> ParseIndentedChildren(string[] lines, ref int i)
        {
            var children = new List<MdBlock>();
            while (i < lines.Length)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) break;
                if (CountLeadingWhitespace(line) < 2) break;

                var trimmed = line.Trim();
                if (UlRegex.IsMatch(trimmed))
                {
                    children.Add(ParseListBlock(lines, ref i, ordered: false));
                    continue;
                }

                if (OlRegex.IsMatch(trimmed))
                {
                    children.Add(ParseListBlock(lines, ref i, ordered: true));
                    continue;
                }

                break;
            }

            return children;
        }

        private static int CountLeadingWhitespace(string line)
        {
            if (string.IsNullOrEmpty(line)) return 0;
            var count = 0;
            foreach (var ch in line)
            {
                if (ch == ' ' || ch == '\t') count++;
                else break;
            }

            return count;
        }

        private static VisualElement RenderBlock(MdBlock block, string baseDirectory)
        {
            switch (block.Kind)
            {
                case MdBlockKind.Heading:
                    return HubMarkdownInline.Render(block.Text, $"hub-md-h{Mathf.Clamp(block.Level, 1, 6)}", baseDirectory);
                case MdBlockKind.Paragraph:
                    return HubMarkdownInline.Render(block.Text, "hub-md-p", baseDirectory);
                case MdBlockKind.Code:
                    return RenderCodeBlock(block.Lines);
                case MdBlockKind.Quote:
                    return RenderQuote(block.Lines, baseDirectory);
                case MdBlockKind.UnorderedList:
                    return RenderList(block, ordered: false, baseDirectory);
                case MdBlockKind.OrderedList:
                    return RenderList(block, ordered: true, baseDirectory);
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
            HubColors.ApplyMarkdownBlock(pre, "hub-md-pre");
            pre.Add(MakePlainLabel(string.Join("\n", lines), "hub-md-code"));
            return pre;
        }

        private static VisualElement RenderQuote(IReadOnlyList<string> lines, string baseDirectory)
        {
            var quote = new VisualElement();
            quote.AddToClassList("hub-md-blockquote");

            foreach (var line in lines)
                quote.Add(HubMarkdownInline.Render(line, "hub-md-quote-line", baseDirectory));

            return quote;
        }

        private static VisualElement RenderList(MdBlock block, bool ordered, string baseDirectory)
        {
            var list = new VisualElement();
            list.AddToClassList(ordered ? "hub-md-ol" : "hub-md-ul");

            var items = block.ListItems;
            if (items == null) return list;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var li = new VisualElement();
                li.AddToClassList("hub-md-li");
                HubMarkdownLayout.ApplyListItem(li);

                var marker = MakePlainLabel(ordered ? $"{i + 1}." : "•", "hub-md-li-marker");
                HubMarkdownLayout.ApplyListMarker(marker);
                li.Add(marker);

                var body = new VisualElement();
                body.AddToClassList("hub-md-li-body");
                HubMarkdownLayout.ApplyListBody(body);

                body.Add(HubMarkdownInline.Render(item.Text, "hub-md-li-text", baseDirectory));

                if (item.Children != null)
                {
                    foreach (var child in item.Children)
                        body.Add(WrapBlock(RenderBlock(child, baseDirectory)));
                }

                li.Add(body);
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
                    cell.Add(MakeTableCellContent(text, isHeader, baseDirectory));
                    rowEl.Add(cell);
                }

                body.Add(rowEl);
            }

            table.Add(body);

            if (totalWidth > 400f)
            {
                var scroll = new ScrollView(ScrollViewMode.Horizontal);
                scroll.AddToClassList("hub-md-table-scroll");
                scroll.contentContainer.style.flexDirection = FlexDirection.Row;
                scroll.contentContainer.Add(table);
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

        private static VisualElement MakeTableCellContent(string text, bool isHeader, string baseDirectory)
        {
            VisualElement content;
            if (isHeader || !ContainsInlineMarkdown(text))
            {
                content = MakePlainLabel(
                    text,
                    isHeader ? "hub-md-cell hub-md-cell--head" : "hub-md-cell");
            }
            else
            {
                content = HubMarkdownInline.Render(text, "hub-md-cell", baseDirectory);
            }

            ConfigureTableCellContent(content);
            return content;
        }

        private static bool ContainsInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf('`') >= 0
                || text.IndexOf('*') >= 0
                || text.IndexOf('[') >= 0
                || text.IndexOf('!') >= 0
                || HubMarkdownInline.ContainsEmoji(text);
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

            text = MeasureLinkedImageRegex.Replace(
                text,
                m => HubShieldsBadge.Format(m.Groups[2].Value, m.Groups[1].Value));
            text = MeasureImageRegex.Replace(text, m => HubShieldsBadge.Format(m.Groups[2].Value, m.Groups[1].Value));
            text = MeasureLinkRegex.Replace(text, "$1");
            text = MeasureCodeRegex.Replace(text, "$1");
            text = MeasureBoldRegex.Replace(text, "$1");
            text = MeasureItalicRegex.Replace(text, "$1");
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

        private static void ConfigureTableCellContent(VisualElement content)
        {
            content.style.whiteSpace = WhiteSpace.Normal;
            content.style.overflow = Overflow.Visible;
            content.style.width = Length.Percent(100);
            content.style.flexGrow = 0;
            content.style.flexShrink = 0;
            content.style.marginTop = 0;
            content.style.marginBottom = 0;
            content.style.paddingTop = 0;
            content.style.paddingBottom = 0;
            content.style.minHeight = 0;
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

        private static VisualElement WrapBlock(VisualElement content)
        {
            if (content == null) return new VisualElement();

            var wrap = new VisualElement();
            wrap.AddToClassList("hub-md-block");
            wrap.style.flexShrink = 0;
            wrap.style.flexGrow = 0;
            wrap.style.flexDirection = FlexDirection.Column;
            wrap.Add(content);
            return wrap;
        }

        internal static void ConfigureInlineLabel(Label label, string contextClass)
        {
            label.AddToClassList("hub-md-run");
            HubMarkdownLayout.ApplyRunLabel(label);
            HubColors.ApplyMarkdownRun(label, contextClass);
        }

        internal static void OpenReadmeLink(string url, string baseDirectory)
            => OpenLink(url, baseDirectory);

        private static void ConfigureBlockLabel(Label label)
        {
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 0;
            label.style.flexGrow = 0;
            label.style.overflow = Overflow.Visible;
        }

        private static Label MakePlainLabel(string text, string className)
        {
            var label = new Label(text) { text = text, enableRichText = false };
            if (!string.IsNullOrEmpty(className)) label.AddToClassList(className);
            ConfigureBlockLabel(label);
            HubColors.ApplyMarkdownBlock(label, className);
            return label;
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

        private sealed class MdListItem
        {
            public string Text { get; }
            public IReadOnlyList<MdBlock> Children { get; }

            public MdListItem(string text, IReadOnlyList<MdBlock> children)
            {
                Text = text;
                Children = children ?? Array.Empty<MdBlock>();
            }
        }

        private sealed class MdBlock
        {
            public MdBlockKind Kind { get; }
            public string Text { get; }
            public int Level { get; }
            public IReadOnlyList<string> Lines { get; }
            public IReadOnlyList<MdListItem> ListItems { get; }

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

            public MdBlock(MdBlockKind kind, List<MdListItem> items)
            {
                Kind = kind;
                ListItems = items;
            }
        }
    }
}
#endif
