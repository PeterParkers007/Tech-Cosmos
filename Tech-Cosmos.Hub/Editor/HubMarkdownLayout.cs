#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// README 布局内联样式（USS 未加载时仍保持正确横排/列表结构）。
    /// </summary>
    internal static class HubMarkdownLayout
    {
        public static void ApplyTree(VisualElement root)
        {
            if (root == null) return;

            root.Query(className: "hub-md-li").ForEach(li => ApplyListItem(li));
            root.Query(className: "hub-md-li-body").ForEach(body => ApplyListBody(body));
            root.Query(className: "hub-md-inline").ForEach(row => ApplyInlineRow(row));
            root.Query<Label>(className: "hub-md-run").ForEach(label => ApplyRunLabel(label));
            root.Query<Label>(className: "hub-md-li-marker").ForEach(marker => ApplyListMarker(marker));
            root.Query(className: "hub-md-ul").ForEach(list => ApplyListContainer(list));
            root.Query(className: "hub-md-ol").ForEach(list => ApplyListContainer(list));
            root.Query(className: "hub-md-blockquote").ForEach(quote => ApplyBlockquote(quote));
        }

        public static void ApplyListItem(VisualElement li)
        {
            if (li == null) return;
            li.style.flexDirection = FlexDirection.Row;
            li.style.alignItems = Align.FlexStart;
            li.style.flexShrink = 0;
            li.style.flexGrow = 0;
            li.style.marginBottom = 6;
        }

        public static void ApplyListBody(VisualElement body)
        {
            if (body == null) return;
            body.style.flexDirection = FlexDirection.Column;
            body.style.flexGrow = 1;
            body.style.flexShrink = 1;
            body.style.minWidth = 0;
            body.style.alignItems = Align.Stretch;
        }

        public static void ApplyListMarker(Label marker)
        {
            if (marker == null) return;
            marker.style.width = 22;
            marker.style.minWidth = 22;
            marker.style.maxWidth = 22;
            marker.style.flexGrow = 0;
            marker.style.flexShrink = 0;
            marker.style.marginTop = 0;
            marker.style.marginBottom = 0;
            marker.style.paddingTop = 0;
            marker.style.paddingBottom = 0;
            marker.style.unityTextAlign = TextAnchor.UpperLeft;
        }

        public static void ApplyInlineRow(VisualElement row)
        {
            if (row == null) return;
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.FlexStart;
            row.style.flexShrink = 0;
        }

        public static void ApplyRunLabel(Label label)
        {
            if (label == null) return;
            label.style.width = StyleKeyword.Auto;
            label.style.minWidth = StyleKeyword.Auto;
            label.style.maxWidth = StyleKeyword.Auto;
            label.style.flexGrow = 0;
            label.style.flexShrink = 0;
            label.style.flexBasis = StyleKeyword.Auto;
            label.style.marginTop = 0;
            label.style.marginBottom = 0;
            label.style.paddingTop = 0;
            label.style.paddingBottom = 0;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Visible;
        }

        private static void ApplyListContainer(VisualElement list)
        {
            if (list == null) return;
            list.style.flexDirection = FlexDirection.Column;
            list.style.flexShrink = 0;
            list.style.marginTop = 4;
            list.style.marginBottom = 8;
        }

        private static void ApplyBlockquote(VisualElement quote)
        {
            if (quote == null) return;
            quote.style.flexDirection = FlexDirection.Column;
            quote.style.borderLeftWidth = 3;
            quote.style.borderLeftColor = new Color(72f / 255f, 118f / 255f, 210f / 255f);
            quote.style.paddingLeft = 12;
            quote.style.marginTop = 8;
            quote.style.marginBottom = 10;
        }
    }
}
#endif
