#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// Web 风配色与关键控件 inline 样式。与 TechCosmosHub.uss 数值一致，确保 USS 未生效时仍保持 Dashboard 观感。
    /// </summary>
    internal static class HubColors
    {
        public static readonly Color PageBg = Rgb(22, 24, 30);
        public static readonly Color HeaderBg = Rgb(28, 32, 42);
        public static readonly Color PanelBg = Rgb(30, 33, 42);
        public static readonly Color PanelBorder = Rgb(45, 50, 62);
        public static readonly Color CardBg = Rgb(24, 27, 35);
        public static readonly Color InputBg = Rgb(24, 27, 35);
        public static readonly Color Title = Rgb(235, 240, 250);
        public static readonly Color Subtitle = Rgb(130, 140, 160);
        public static readonly Color BodyText = Rgb(210, 216, 228);
        public static readonly Color Muted = Rgb(120, 130, 150);
        public static readonly Color Primary = Rgb(79, 140, 255);
        public static readonly Color PrimaryText = Color.white;
        public static readonly Color TabIdle = Rgb(140, 148, 168);
        public static readonly Color TabActive = Rgb(100, 165, 255);
        public static readonly Color TabBg = Rgb(36, 40, 52);
        public static readonly Color GhostBg = Rgb(40, 44, 56);
        public static readonly Color GhostBorder = Rgb(55, 60, 74);
        public static readonly Color GhostText = Rgb(180, 188, 205);
        public static readonly Color AccentBg = Rgb(50, 120, 90);
        public static readonly Color AccentBorder = Rgb(62, 140, 105);
        public static readonly Color DangerBorder = Rgb(200, 80, 80);
        public static readonly Color DangerText = Rgb(230, 120, 120);
        public static readonly Color CodeBg = Rgb(18, 20, 28);
        public static readonly Color CodeText = Rgb(186, 196, 214);
        public static readonly Color ChipBg = Rgb(38, 42, 54);
        public static readonly Color SelectedBg = Rgb(42, 52, 72);
        public static readonly Color SelectedBorder = Primary;

        private static Color Rgb(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f);

        public static void ApplyShell(
            VisualElement root,
            VisualElement header,
            VisualElement tabs,
            VisualElement body,
            VisualElement sidebar,
            VisualElement detail)
        {
            if (root != null)
                root.style.backgroundColor = PageBg;
            if (header != null)
            {
                header.style.backgroundColor = HeaderBg;
                header.style.borderBottomWidth = 1;
                header.style.borderBottomColor = Rgb(45, 50, 64);
            }
            if (tabs != null)
                tabs.style.backgroundColor = PageBg;
            if (body != null)
            {
                body.style.paddingLeft = 12;
                body.style.paddingRight = 12;
                body.style.paddingBottom = 12;
            }
            if (sidebar != null)
            {
                sidebar.style.backgroundColor = PanelBg;
                sidebar.style.borderTopWidth = sidebar.style.borderRightWidth =
                    sidebar.style.borderBottomWidth = sidebar.style.borderLeftWidth = 1;
                sidebar.style.borderTopColor = sidebar.style.borderRightColor =
                    sidebar.style.borderBottomColor = sidebar.style.borderLeftColor = PanelBorder;
                sidebar.style.borderTopLeftRadius = sidebar.style.borderTopRightRadius =
                    sidebar.style.borderBottomLeftRadius = sidebar.style.borderBottomRightRadius = 10;
                sidebar.style.paddingTop = sidebar.style.paddingBottom =
                    sidebar.style.paddingLeft = sidebar.style.paddingRight = 12;
            }
            if (detail != null)
            {
                detail.style.backgroundColor = PanelBg;
                detail.style.borderTopWidth = detail.style.borderRightWidth =
                    detail.style.borderBottomWidth = detail.style.borderLeftWidth = 1;
                detail.style.borderTopColor = detail.style.borderRightColor =
                    detail.style.borderBottomColor = detail.style.borderLeftColor = PanelBorder;
                detail.style.borderTopLeftRadius = detail.style.borderTopRightRadius =
                    detail.style.borderBottomLeftRadius = detail.style.borderBottomRightRadius = 10;
                detail.style.paddingTop = detail.style.paddingBottom =
                    detail.style.paddingLeft = detail.style.paddingRight = 20;
            }
        }

        public static void ApplyTitle(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 18;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Title;
        }

        public static void ApplySubtitle(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.color = Subtitle;
            label.style.marginTop = 2;
        }

        public static void ApplyStatChip(VisualElement chip, Label value, Label caption)
        {
            if (chip != null)
            {
                chip.style.flexDirection = FlexDirection.Row;
                chip.style.alignItems = Align.Center;
                chip.style.backgroundColor = ChipBg;
                chip.style.borderTopLeftRadius = chip.style.borderTopRightRadius =
                    chip.style.borderBottomLeftRadius = chip.style.borderBottomRightRadius = 12;
                chip.style.paddingTop = 4;
                chip.style.paddingBottom = 4;
                chip.style.paddingLeft = 10;
                chip.style.paddingRight = 10;
                chip.style.marginRight = 8;
            }
            if (value != null)
            {
                value.style.fontSize = 13;
                value.style.unityFontStyleAndWeight = FontStyle.Bold;
                value.style.color = Rgb(220, 228, 240);
                value.style.marginRight = 4;
            }
            if (caption != null)
            {
                caption.style.fontSize = 10;
                caption.style.color = Muted;
            }
        }

        public static void ApplyTab(Button tab, bool active)
        {
            if (tab == null) return;
            tab.style.height = 32;
            tab.style.paddingLeft = 16;
            tab.style.paddingRight = 16;
            tab.style.marginRight = 6;
            tab.style.borderTopWidth = tab.style.borderRightWidth =
                tab.style.borderLeftWidth = 0;
            tab.style.borderBottomWidth = active ? 2 : 0;
            tab.style.borderBottomColor = Primary;
            tab.style.borderTopLeftRadius = 8;
            tab.style.borderTopRightRadius = 8;
            tab.style.backgroundColor = active ? TabBg : StyleKeyword.Null;
            tab.style.color = active ? TabActive : TabIdle;
            tab.style.fontSize = 12;
            tab.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
        }

        public static void RefreshTab(Button tab, bool active)
        {
            ApplyTab(tab, active);
            HubHoverEffects.BindTab(tab);
        }

        public static void RefreshButton(Button button, string classNames)
        {
            ApplyButton(button, classNames);
            HubHoverEffects.BindButton(button, classNames);
        }

        public static void RefreshModeToggle(Button button, bool active)
        {
            ApplyModeToggle(button, active);
            HubHoverEffects.BindModeToggle(button);
        }

        public static void RefreshDepLinkButton(Button button)
        {
            ApplyDepLinkButton(button);
            HubHoverEffects.BindDepLink(button);
        }

        public static void RefreshListItem(VisualElement item, bool selected)
        {
            ApplyListItem(item, selected);
            HubHoverEffects.BindListItem(item);
        }

        public static void RefreshToggle(Toggle toggle)
        {
            ApplyToggle(toggle);
            HubHoverEffects.BindToggle(toggle);
        }

        public static void RefreshGitVersionTrigger(Button trigger)
        {
            ApplyGitVersionTrigger(trigger);
            HubHoverEffects.BindGitVersionTrigger(trigger);
        }

        public static void RefreshMarkdownLink(Label label)
        {
            ApplyMarkdownLink(label);
            HubHoverEffects.BindMarkdownLink(label);
        }

        public static void ApplyButton(Button button, string classNames)
        {
            if (button == null) return;
            button.style.height = 32;
            button.style.paddingLeft = 14;
            button.style.paddingRight = 14;
            button.style.marginLeft = 6;
            button.style.marginTop = 4;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius =
                button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 8;
            button.style.borderTopWidth = button.style.borderRightWidth =
                button.style.borderBottomWidth = button.style.borderLeftWidth = 1;
            button.style.fontSize = 11;

            if (button.enabledSelf == false)
            {
                button.style.backgroundColor = Rgb(50, 58, 75);
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = Rgb(55, 62, 78);
                button.style.color = Rgb(100, 108, 125);
                return;
            }

            if (classNames != null && classNames.Contains("hub-btn--primary"))
            {
                button.style.backgroundColor = Primary;
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = Rgb(90, 150, 255);
                button.style.color = PrimaryText;
            }
            else if (classNames != null && classNames.Contains("hub-btn--accent"))
            {
                button.style.backgroundColor = AccentBg;
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = AccentBorder;
                button.style.color = PrimaryText;
            }
            else if (classNames != null && classNames.Contains("hub-btn--danger"))
            {
                button.style.backgroundColor = StyleKeyword.Null;
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = DangerBorder;
                button.style.color = DangerText;
            }
            else
            {
                button.style.backgroundColor = GhostBg;
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = GhostBorder;
                button.style.color = GhostText;
            }
        }

        public static void ApplySectionTitle(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Muted;
            label.style.marginBottom = 8;
            label.style.marginTop = 8;
        }

        public static void ApplyDetailTitle(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 22;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Rgb(240, 244, 252);
        }

        public static void ApplyDetailDesc(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 13;
            label.style.color = Rgb(160, 168, 188);
            label.style.marginTop = 4;
            label.style.whiteSpace = WhiteSpace.Normal;
        }

        public static void ApplyReadmeCard(VisualElement card)
        {
            if (card == null) return;
            card.style.backgroundColor = CardBg;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
                card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 10;
            card.style.borderTopWidth = card.style.borderRightWidth =
                card.style.borderBottomWidth = card.style.borderLeftWidth = 1;
            card.style.borderTopColor = card.style.borderRightColor =
                card.style.borderBottomColor = card.style.borderLeftColor = Rgb(42, 48, 60);
            card.style.paddingTop = 20;
            card.style.paddingBottom = 20;
            card.style.paddingLeft = 22;
            card.style.paddingRight = 22;
            card.style.marginTop = 8;
        }

        public static void ApplyDepSection(VisualElement section)
        {
            if (section == null) return;
            section.style.backgroundColor = CardBg;
            section.style.borderTopLeftRadius = section.style.borderTopRightRadius =
                section.style.borderBottomLeftRadius = section.style.borderBottomRightRadius = 8;
            section.style.borderTopWidth = section.style.borderRightWidth =
                section.style.borderBottomWidth = section.style.borderLeftWidth = 1;
            section.style.borderTopColor = section.style.borderRightColor =
                section.style.borderBottomColor = section.style.borderLeftColor = Rgb(42, 48, 60);
            section.style.paddingTop = 12;
            section.style.paddingBottom = 12;
            section.style.paddingLeft = 14;
            section.style.paddingRight = 14;
            section.style.marginBottom = 12;
        }

        public static void ApplyGitVersionCard(VisualElement card)
        {
            if (card == null) return;
            card.style.backgroundColor = CardBg;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
                card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 10;
            card.style.borderTopWidth = card.style.borderRightWidth =
                card.style.borderBottomWidth = card.style.borderLeftWidth = 1;
            card.style.borderTopColor = card.style.borderRightColor =
                card.style.borderBottomColor = card.style.borderLeftColor = Rgb(42, 48, 60);
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
        }

        public static void ApplyGitVersionTitle(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.color = BodyText;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = 0.4f;
        }

        public static void ApplyGitVersionCount(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 10;
            label.style.paddingTop = 3;
            label.style.paddingBottom = 3;
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.borderTopLeftRadius = label.style.borderTopRightRadius =
                label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 10;
            label.style.backgroundColor = Rgb(38, 44, 58);
            label.style.color = Rgb(130, 145, 175);
        }

        public static void ApplyGitVersionTrigger(Button trigger)
        {
            if (trigger == null) return;
            trigger.style.height = 38;
            trigger.style.width = Length.Percent(100);
            trigger.style.flexDirection = FlexDirection.Row;
            trigger.style.alignItems = Align.Center;
            trigger.style.paddingLeft = 12;
            trigger.style.paddingRight = 12;
            trigger.style.marginLeft = 0;
            trigger.style.marginRight = 0;
            trigger.style.marginTop = 0;
            trigger.style.marginBottom = 0;
            trigger.style.borderTopLeftRadius = trigger.style.borderTopRightRadius =
                trigger.style.borderBottomLeftRadius = trigger.style.borderBottomRightRadius = 8;
            trigger.style.borderTopWidth = trigger.style.borderRightWidth =
                trigger.style.borderBottomWidth = trigger.style.borderLeftWidth = 1;
            trigger.style.borderTopColor = trigger.style.borderRightColor =
                trigger.style.borderBottomColor = trigger.style.borderLeftColor = Rgb(50, 56, 70);
            trigger.style.backgroundColor = InputBg;
            trigger.style.unityTextAlign = TextAnchor.MiddleLeft;
        }

        public static void ApplyGitVersionTriggerHover(Button trigger)
        {
            if (trigger == null) return;
            trigger.style.borderTopColor = trigger.style.borderRightColor =
                trigger.style.borderBottomColor = trigger.style.borderLeftColor = Rgb(79, 120, 200);
            trigger.style.backgroundColor = Rgb(30, 34, 44);
        }

        public static void ApplyGitVersionTriggerPressed(Button trigger)
        {
            ApplyGitVersionTriggerHover(trigger);
        }

        public static void ApplyGitVersionIcon(Label label, bool latest)
        {
            if (label == null) return;
            label.style.width = 22;
            label.style.height = 22;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.fontSize = 11;
            label.style.borderTopLeftRadius = label.style.borderTopRightRadius =
                label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 6;
            label.style.flexShrink = 0;
            if (latest)
            {
                label.style.backgroundColor = new Color(79f / 255f, 140f / 255f, 255f / 255f, 0.16f);
                label.style.color = Rgb(120, 175, 255);
            }
            else
            {
                label.style.backgroundColor = new Color(62f / 255f, 207f / 255f, 142f / 255f, 0.14f);
                label.style.color = Rgb(90, 210, 155);
            }
        }

        public static void ApplyGitVersionValuePrimary(Label label, bool latest)
        {
            if (label == null) return;
            label.style.fontSize = 13;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = latest ? Rgb(210, 220, 240) : Rgb(120, 200, 160);
        }

        public static void ApplyGitVersionValueSub(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 10;
            label.style.color = Muted;
            label.style.marginTop = 1;
        }

        public static void ApplyGitVersionChevron(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 12;
            label.style.color = Rgb(130, 140, 160);
            label.style.flexShrink = 0;
            label.style.marginLeft = 4;
        }

        public static void ApplyGitVersionHint(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 10;
            label.style.color = Muted;
            label.style.marginTop = 2;
            label.style.whiteSpace = WhiteSpace.Normal;
        }

        public static void ApplyGitVersionLoading(VisualElement row)
        {
            if (row == null) return;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 38;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius =
                row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 8;
            row.style.borderTopWidth = row.style.borderRightWidth =
                row.style.borderBottomWidth = row.style.borderLeftWidth = 1;
            row.style.borderTopColor = row.style.borderRightColor =
                row.style.borderBottomColor = row.style.borderLeftColor = Rgb(42, 48, 60);
            row.style.backgroundColor = Rgb(20, 23, 30);
        }

        public static void ApplyGitVersionLoadingText(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.color = Rgb(130, 140, 160);
            label.style.marginLeft = 8;
        }

        public static void ApplyGitVersionLoadingDot(VisualElement dot)
        {
            if (dot == null) return;
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
                dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 4;
            dot.style.backgroundColor = Primary;
            dot.style.flexShrink = 0;
        }

        public static void ApplyListItem(VisualElement item, bool selected)
        {
            if (item == null) return;
            item.style.borderTopLeftRadius = item.style.borderBottomLeftRadius = 8;
            item.style.borderTopRightRadius = item.style.borderBottomRightRadius = 8;
            item.style.paddingTop = 6;
            item.style.paddingBottom = 6;
            item.style.paddingLeft = 10;
            item.style.paddingRight = 8;
            item.style.marginBottom = 2;
            item.style.borderLeftWidth = 3;
            item.style.backgroundColor = selected ? SelectedBg : new Color(0, 0, 0, 0);
            item.style.borderLeftColor = selected ? SelectedBorder : new Color(0, 0, 0, 0);
        }

        public static void ApplyCategoryLabel(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 10;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Muted;
            label.style.marginTop = 10;
            label.style.marginBottom = 4;
        }

        public static void ApplyListItemName(Label label, bool selected)
        {
            if (label == null) return;
            label.style.fontSize = 12;
            label.style.color = selected ? Rgb(235, 240, 255) : BodyText;
            label.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            label.style.flexGrow = 1;
        }

        public static void ApplyMarkdownBlock(VisualElement element, string className)
        {
            if (element == null || string.IsNullOrEmpty(className)) return;

            if (className.Contains("hub-md-pre"))
            {
                element.style.backgroundColor = CodeBg;
                element.style.borderTopLeftRadius = element.style.borderTopRightRadius =
                    element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = 8;
                element.style.borderTopWidth = element.style.borderRightWidth =
                    element.style.borderBottomWidth = element.style.borderLeftWidth = 1;
                element.style.borderTopColor = element.style.borderRightColor =
                    element.style.borderBottomColor = element.style.borderLeftColor = Rgb(42, 48, 62);
                element.style.paddingTop = 12;
                element.style.paddingBottom = 12;
                element.style.paddingLeft = 14;
                element.style.paddingRight = 14;
                element.style.marginTop = 8;
                element.style.marginBottom = 12;
                return;
            }

            if (element is not Label label) return;

            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Visible;

            if (className.Contains("hub-md-code"))
            {
                label.style.fontSize = 12;
                label.style.color = CodeText;
                return;
            }

            if (className.Contains("hub-md-h1"))
            {
                label.style.fontSize = 24;
                label.style.color = Rgb(240, 244, 252);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 4;
                label.style.marginBottom = 12;
                return;
            }

            if (className.Contains("hub-md-h2"))
            {
                label.style.fontSize = 19;
                label.style.color = Rgb(236, 240, 248);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 20;
                label.style.marginBottom = 10;
                return;
            }

            if (className.Contains("hub-md-h3"))
            {
                label.style.fontSize = 16;
                label.style.color = Rgb(228, 232, 240);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 16;
                label.style.marginBottom = 8;
                return;
            }

            if (className.Contains("hub-md-h4"))
            {
                label.style.fontSize = 14;
                label.style.color = Rgb(220, 226, 236);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                return;
            }

            if (className.Contains("hub-md-h5"))
            {
                label.style.fontSize = 13;
                label.style.color = Rgb(210, 216, 228);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                return;
            }

            if (className.Contains("hub-md-h6"))
            {
                label.style.fontSize = 12;
                label.style.color = Rgb(170, 178, 195);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                return;
            }

            if (className.Contains("hub-md-code-inline"))
            {
                label.style.fontSize = 12;
                label.style.color = Rgb(230, 180, 120);
                label.style.backgroundColor = CodeBg;
                label.style.borderTopLeftRadius = label.style.borderTopRightRadius =
                    label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 4;
                label.style.paddingLeft = 4;
                label.style.paddingRight = 4;
                return;
            }

            if (className.Contains("hub-md-link-text"))
            {
                label.style.color = Rgb(120, 170, 255);
                label.style.fontSize = 13;
                return;
            }

            if (className.Contains("hub-md-empty") || className.Contains("hub-md-truncate"))
            {
                label.style.fontSize = 12;
                label.style.color = Muted;
                label.style.unityFontStyleAndWeight = FontStyle.Italic;
                return;
            }

            if (className.Contains("hub-md-cell"))
            {
                label.style.fontSize = 12;
                label.style.color = Rgb(190, 198, 212);
                return;
            }

            if (className.Contains("hub-md-cell--head"))
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.color = Rgb(220, 228, 240);
                return;
            }

            if (className.Contains("hub-md-quote-line")
                || className.Contains("hub-md-p")
                || className.Contains("hub-md-li-text")
                || className.Contains("hub-md-emoji")
                || className.Contains("hub-md-symbol"))
            {
                label.style.fontSize = 13;
                label.style.color = Rgb(198, 204, 218);
                label.style.marginBottom = 6;
            }

            if (className.Contains("hub-md-li-marker"))
            {
                label.style.fontSize = 13;
                label.style.color = Rgb(120, 140, 180);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginBottom = 0;
            }
        }

        public static void ApplyMarkdownRun(Label label, string contextClass)
        {
            if (label == null || string.IsNullOrEmpty(contextClass)) return;

            label.style.marginTop = 0;
            label.style.marginBottom = 0;

            if (contextClass.Contains("hub-md-h1"))
            {
                label.style.fontSize = 24;
                label.style.color = Rgb(240, 244, 252);
                return;
            }

            if (contextClass.Contains("hub-md-h2"))
            {
                label.style.fontSize = 19;
                label.style.color = Rgb(236, 240, 248);
                return;
            }

            if (contextClass.Contains("hub-md-h3"))
            {
                label.style.fontSize = 16;
                label.style.color = Rgb(228, 232, 240);
                return;
            }

            if (contextClass.Contains("hub-md-h4"))
            {
                label.style.fontSize = 14;
                label.style.color = Rgb(220, 226, 236);
                return;
            }

            if (contextClass.Contains("hub-md-h5"))
            {
                label.style.fontSize = 13;
                label.style.color = Rgb(210, 216, 228);
                return;
            }

            if (contextClass.Contains("hub-md-h6"))
            {
                label.style.fontSize = 12;
                label.style.color = Rgb(170, 178, 195);
                return;
            }

            if (contextClass.Contains("hub-md-quote-line"))
            {
                label.style.fontSize = 13;
                label.style.color = Rgb(170, 180, 200);
                return;
            }

            if (contextClass.Contains("hub-md-cell"))
            {
                label.style.fontSize = 12;
                label.style.color = Rgb(190, 198, 212);
                return;
            }

            label.style.fontSize = 13;
            label.style.color = Rgb(198, 204, 218);
        }

        public static void ApplyMarkdownSymbol(Label label, string styleClass)
        {
            if (label == null) return;

            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginRight = 2;

            if (styleClass != null && styleClass.Contains("hub-md-symbol--ok"))
                label.style.color = Rgb(62, 207, 142);
            else if (styleClass != null && styleClass.Contains("hub-md-symbol--fail"))
                label.style.color = Rgb(220, 90, 90);
            else if (styleClass != null && styleClass.Contains("hub-md-symbol--warn"))
                label.style.color = Rgb(230, 180, 80);
            else if (styleClass != null && styleClass.Contains("hub-md-symbol--star"))
                label.style.color = Rgb(230, 200, 90);
            else
                label.style.color = Rgb(198, 204, 218);
        }

        public static void ApplySearchField(VisualElement field)
        {
            if (field == null) return;
            field.style.backgroundColor = InputBg;
            field.style.borderTopColor = field.style.borderRightColor =
                field.style.borderBottomColor = field.style.borderLeftColor = Rgb(50, 56, 70);
            field.style.borderTopLeftRadius = field.style.borderTopRightRadius =
                field.style.borderBottomLeftRadius = field.style.borderBottomRightRadius = 8;
        }

        public static void ApplyFieldLabel(Label label, bool stackAbove = false)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.color = Rgb(130, 138, 155);
            label.style.marginBottom = 4;
            label.style.whiteSpace = WhiteSpace.Normal;
            if (stackAbove)
            {
                label.style.width = StyleKeyword.Auto;
                label.style.minWidth = 0;
            }
            else
            {
                label.style.width = 72;
                label.style.minWidth = 72;
            }
        }

        public static void ApplySidebarHint(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 10;
            label.style.color = Rgb(100, 110, 128);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 4;
            label.style.marginBottom = 4;
        }

        public static void ApplyModeToggle(Button button, bool active)
        {
            if (button == null) return;
            button.style.height = 28;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.marginRight = 4;
            button.style.marginBottom = 4;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius =
                button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 8;
            button.style.borderTopWidth = button.style.borderRightWidth =
                button.style.borderBottomWidth = button.style.borderLeftWidth = 0;
            button.style.fontSize = 11;
            button.style.backgroundColor = active ? TabBg : new Color(0, 0, 0, 0);
            button.style.color = active ? TabActive : TabIdle;
            button.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
        }

        public static void ApplyBadge(Label badge, string classNames)
        {
            if (badge == null) return;
            badge.style.fontSize = 10;
            badge.style.paddingTop = 4;
            badge.style.paddingBottom = 4;
            badge.style.paddingLeft = 10;
            badge.style.paddingRight = 10;
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius =
                badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 10;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.marginRight = 6;

            if (classNames != null && classNames.Contains("hub-badge--installed"))
            {
                badge.style.backgroundColor = new Color(62f / 255f, 207f / 255f, 142f / 255f, 0.18f);
                badge.style.color = Rgb(62, 207, 142);
            }
            else if (classNames != null && classNames.Contains("hub-badge--local"))
            {
                badge.style.backgroundColor = new Color(245f / 255f, 166f / 255f, 35f / 255f, 0.18f);
                badge.style.color = Rgb(245, 166, 35);
            }
            else if (classNames != null && classNames.Contains("hub-badge--pending"))
            {
                badge.style.backgroundColor = new Color(245f / 255f, 166f / 255f, 35f / 255f, 0.15f);
                badge.style.color = Rgb(245, 166, 35);
            }
            else if (classNames != null && classNames.Contains("hub-badge--ready"))
            {
                badge.style.backgroundColor = new Color(62f / 255f, 207f / 255f, 142f / 255f, 0.18f);
                badge.style.color = Rgb(62, 207, 142);
            }
            else
            {
                badge.style.backgroundColor = new Color(120f / 255f, 130f / 255f, 150f / 255f, 0.15f);
                badge.style.color = Rgb(130, 140, 155);
            }
        }

        public static void ApplyDepLinkButton(Button button)
        {
            if (button == null) return;
            button.style.backgroundColor = new Color(0, 0, 0, 0);
            button.style.borderTopWidth = button.style.borderRightWidth =
                button.style.borderBottomWidth = button.style.borderLeftWidth = 0;
            button.style.color = Rgb(186, 196, 214);
            button.style.fontSize = 12;
            button.style.height = 22;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 8;
        }

        public static void ApplyChip(Label label, bool category = false)
        {
            if (label == null) return;
            label.style.fontSize = 10;
            label.style.paddingTop = 3;
            label.style.paddingBottom = 3;
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.borderTopLeftRadius = label.style.borderTopRightRadius =
                label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 10;
            label.style.marginRight = 6;
            label.style.marginBottom = 4;
            label.style.whiteSpace = WhiteSpace.Normal;
            if (category)
            {
                label.style.backgroundColor = Rgb(55, 65, 95);
                label.style.color = Rgb(140, 175, 255);
            }
            else
            {
                label.style.backgroundColor = Rgb(45, 50, 64);
                label.style.color = Rgb(160, 170, 190);
            }
        }

        public static void ApplyStatusDot(VisualElement dot, bool installed, bool local = false)
        {
            if (dot == null) return;
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
                dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 4;
            dot.style.marginRight = 8;
            dot.style.flexShrink = 0;
            if (installed)
                dot.style.backgroundColor = Rgb(62, 207, 142);
            else if (local)
                dot.style.backgroundColor = Rgb(245, 166, 35);
            else
                dot.style.backgroundColor = Rgb(90, 98, 115);
        }

        public static void ApplyCheckItem(VisualElement row, Label icon, Label text, bool ok)
        {
            if (row != null)
            {
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 6;
                row.style.paddingBottom = 6;
            }
            if (icon != null)
            {
                icon.style.width = 18;
                icon.style.fontSize = 12;
                icon.style.unityFontStyleAndWeight = FontStyle.Bold;
                icon.style.marginRight = 8;
                icon.style.color = ok ? Rgb(62, 207, 142) : Rgb(220, 90, 90);
            }
            if (text != null)
            {
                text.style.fontSize = 11;
                text.style.color = Rgb(170, 178, 195);
                text.style.flexGrow = 1;
                text.style.whiteSpace = WhiteSpace.Normal;
            }
        }

        public static void ApplyTextField(TextField field)
        {
            if (field == null) return;
            field.style.flexGrow = 1;
            field.style.minWidth = 80;
            field.style.height = 26;
            field.style.backgroundColor = InputBg;
            field.style.borderTopColor = field.style.borderRightColor =
                field.style.borderBottomColor = field.style.borderLeftColor = Rgb(50, 56, 70);
            field.style.borderTopLeftRadius = field.style.borderTopRightRadius =
                field.style.borderBottomLeftRadius = field.style.borderBottomRightRadius = 6;
            field.style.color = BodyText;
            field.style.fontSize = 12;
        }

        public static void ApplyPopupField(PopupField<string> field)
        {
            if (field == null) return;
            field.style.flexGrow = 1;
            field.style.minWidth = 120;
            field.style.height = 26;
            field.style.backgroundColor = InputBg;
            field.style.borderTopColor = field.style.borderRightColor =
                field.style.borderBottomColor = field.style.borderLeftColor = Rgb(50, 56, 70);
            field.style.borderTopLeftRadius = field.style.borderTopRightRadius =
                field.style.borderBottomLeftRadius = field.style.borderBottomRightRadius = 6;
            field.style.color = BodyText;
            field.style.fontSize = 12;
        }

        public static void ApplyToggle(Toggle toggle)
        {
            if (toggle == null) return;
            toggle.style.fontSize = 12;
            toggle.style.color = BodyText;
            toggle.style.marginTop = 4;
            toggle.style.marginBottom = 4;
            toggle.style.borderTopLeftRadius = toggle.style.borderTopRightRadius =
                toggle.style.borderBottomLeftRadius = toggle.style.borderBottomRightRadius = 6;
            toggle.style.paddingLeft = 4;
            toggle.style.paddingRight = 4;
        }

        public static void ApplyMarkdownLink(Label label)
        {
            if (label == null) return;
            label.style.color = Rgb(120, 170, 255);
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            label.style.borderTopLeftRadius = label.style.borderTopRightRadius =
                label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 4;
            label.style.paddingLeft = 2;
            label.style.paddingRight = 2;
        }

        public static void ApplyWarningBox(VisualElement box)
        {
            if (box == null) return;
            box.style.backgroundColor = new Color(245f / 255f, 166f / 255f, 35f / 255f, 0.1f);
            box.style.borderTopWidth = box.style.borderRightWidth =
                box.style.borderBottomWidth = box.style.borderLeftWidth = 1;
            box.style.borderTopColor = box.style.borderRightColor =
                box.style.borderBottomColor = box.style.borderLeftColor =
                    new Color(245f / 255f, 166f / 255f, 35f / 255f, 0.35f);
            box.style.borderTopLeftRadius = box.style.borderTopRightRadius =
                box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 8;
            box.style.paddingTop = 10;
            box.style.paddingBottom = 10;
            box.style.paddingLeft = 12;
            box.style.paddingRight = 12;
            box.style.marginTop = 10;
        }

        public static void ApplyWarningText(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.color = Rgb(230, 180, 100);
            label.style.whiteSpace = WhiteSpace.Normal;
        }

        public static void ApplyEmptyState(VisualElement empty)
        {
            if (empty == null) return;
            empty.style.flexGrow = 1;
            empty.style.alignItems = Align.Center;
            empty.style.justifyContent = Justify.Center;
            empty.style.paddingTop = 40;
            empty.style.paddingBottom = 40;
        }

        public static void ApplyEmptyText(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 13;
            label.style.color = Rgb(120, 130, 148);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 8;
        }

        public static void ApplyDepHeading(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 10;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Muted;
            label.style.marginTop = 6;
            label.style.marginBottom = 6;
        }

        public static void ApplyDepDetail(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.color = Muted;
            label.style.marginLeft = 6;
        }

        public static void ApplyDepBlockReason(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 12;
            label.style.color = Rgb(240, 160, 160);
            label.style.marginBottom = 10;
            label.style.whiteSpace = WhiteSpace.Normal;
        }

        public static void ApplyTreeRoot(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Rgb(120, 180, 255);
            label.style.marginTop = 4;
        }

        public static void ApplyTreeItem(Label label)
        {
            if (label == null) return;
            label.style.fontSize = 11;
            label.style.color = Rgb(160, 170, 190);
            label.style.marginLeft = 4;
        }
    }
}
#endif
