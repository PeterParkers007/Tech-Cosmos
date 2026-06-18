#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// 仅写入 flex/尺寸/overflow，不写颜色或圆角，避免覆盖 USS 视觉样式。
    /// USS 未加载时仍保证 Hub 排版与加依赖之前一致。
    /// </summary>
    internal static class HubShellLayout
    {
        public static void ApplyRoot(VisualElement root)
        {
            if (root == null) return;
            root.style.flexGrow = 1;
            root.style.flexShrink = 1;
            root.style.flexDirection = FlexDirection.Column;
            root.style.minHeight = 0;
        }

        public static void ApplyHeader(VisualElement header)
        {
            if (header == null) return;
            header.style.flexDirection = FlexDirection.Row;
            header.style.flexWrap = Wrap.Wrap;
            header.style.alignItems = Align.Center;
            header.style.flexShrink = 0;
        }

        public static void ApplyTabs(VisualElement tabs)
        {
            if (tabs == null) return;
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.flexWrap = Wrap.Wrap;
            tabs.style.flexShrink = 0;
        }

        public static void ApplyBody(VisualElement body)
        {
            if (body == null) return;
            body.style.flexGrow = 1;
            body.style.flexShrink = 1;
            body.style.flexDirection = FlexDirection.Row;
            body.style.minHeight = 100;
        }

        public static void ApplySidebar(VisualElement sidebar)
        {
            if (sidebar == null) return;
            sidebar.style.width = 280;
            sidebar.style.minWidth = 200;
            sidebar.style.maxWidth = 320;
            sidebar.style.flexShrink = 0;
            sidebar.style.flexGrow = 0;
            sidebar.style.flexDirection = FlexDirection.Column;
            sidebar.style.minHeight = 0;
            sidebar.style.overflow = Overflow.Hidden;
        }

        public static void ApplySidebarSearchWrap(VisualElement wrap)
        {
            if (wrap == null) return;
            wrap.style.flexShrink = 0;
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.width = Length.Percent(100);
            wrap.style.minWidth = 0;
            wrap.style.marginBottom = 10;
        }

        public static void ApplySidebarScroll(ScrollView scroll)
        {
            if (scroll == null) return;
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 40;
        }

        public static void ApplySidebarFooter(VisualElement footer)
        {
            if (footer == null) return;
            footer.style.flexShrink = 0;
            footer.style.flexGrow = 0;
            footer.style.flexDirection = FlexDirection.Column;
            footer.style.width = Length.Percent(100);
            footer.style.minWidth = 0;
            footer.style.overflow = Overflow.Visible;
            footer.style.marginTop = 10;
            footer.style.paddingTop = 10;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new UnityEngine.Color(45f / 255f, 50f / 255f, 62f / 255f);
        }

        public static void ApplyImportMode(VisualElement block)
        {
            if (block == null) return;
            block.style.flexDirection = FlexDirection.Column;
            block.style.flexShrink = 0;
            block.style.width = Length.Percent(100);
            block.style.minWidth = 0;
            block.style.marginBottom = 8;
        }

        public static void ApplyImportModeRow(VisualElement row)
        {
            if (row == null) return;
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
            row.style.width = Length.Percent(100);
        }

        public static void ApplyBatchRow(VisualElement row)
        {
            if (row == null) return;
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
            row.style.width = Length.Percent(100);
            row.style.marginBottom = 8;
        }

        public static void ApplyDetailMeta(VisualElement meta)
        {
            if (meta == null) return;
            meta.style.flexDirection = FlexDirection.Row;
            meta.style.flexWrap = Wrap.Wrap;
            meta.style.alignItems = Align.Center;
            meta.style.marginTop = 10;
        }

        public static void ApplyChecklist(VisualElement list)
        {
            if (list == null) return;
            list.style.flexDirection = FlexDirection.Column;
            list.style.marginTop = 8;
            list.style.marginBottom = 8;
        }

        public static void ApplyFieldRow(VisualElement row)
        {
            if (row == null) return;
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 6;
            row.style.width = Length.Percent(100);
        }

        public static void ApplyDetailBottom(VisualElement bottom)
        {
            if (bottom == null) return;
            bottom.style.flexShrink = 0;
            bottom.style.flexDirection = FlexDirection.Column;
            bottom.style.marginTop = 8;
            bottom.style.paddingTop = 12;
            bottom.style.borderTopWidth = 1;
            bottom.style.borderTopColor = new UnityEngine.Color(45f / 255f, 50f / 255f, 62f / 255f);
        }

        public static void ApplyDepList(VisualElement list)
        {
            if (list == null) return;
            list.style.flexDirection = FlexDirection.Column;
        }

        public static void ApplyDetailPanel(VisualElement detail)
        {
            if (detail == null) return;
            detail.style.flexGrow = 1;
            detail.style.flexShrink = 1;
            detail.style.flexDirection = FlexDirection.Column;
            detail.style.minWidth = 0;
            detail.style.minHeight = 0;
            detail.style.overflow = Overflow.Hidden;
        }

        public static void ApplyDetailShell(
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

        public static void ApplyBtnRow(VisualElement row)
        {
            if (row == null) return;
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
        }

        public static void ApplyDetailHeader(VisualElement header)
        {
            if (header == null) return;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.FlexStart;
        }

        public static void ApplyStats(VisualElement stats)
        {
            if (stats == null) return;
            stats.style.flexDirection = FlexDirection.Row;
            stats.style.flexWrap = Wrap.Wrap;
            stats.style.alignItems = Align.Center;
        }

        public static void ApplyHeaderActions(VisualElement actions)
        {
            if (actions == null) return;
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.flexWrap = Wrap.Wrap;
            actions.style.alignItems = Align.Center;
            actions.style.flexShrink = 0;
        }

        public static void ApplyListItem(VisualElement item)
        {
            if (item == null) return;
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.minHeight = 32;
        }

        public static void ApplyDepRow(VisualElement row)
        {
            if (row == null) return;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexWrap = Wrap.Wrap;
            row.style.flexShrink = 0;
        }
    }
}
#endif
