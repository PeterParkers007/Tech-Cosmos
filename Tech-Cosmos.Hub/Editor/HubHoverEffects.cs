#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// C# 悬停反馈：inline 样式会盖掉 USS 的 :hover，因此用 PointerEnter/Leave 模拟。
    /// </summary>
    internal static class HubHoverEffects
    {
        private sealed class Binding
        {
            public EventCallback<PointerEnterEvent> Enter;
            public EventCallback<PointerLeaveEvent> Leave;
        }

        public static void BindButton(Button button, string classNames)
        {
            if (button == null) return;
            Unbind(button);

            void Enter(PointerEnterEvent _)
            {
                if (!button.enabledSelf) return;
                ApplyButtonHover(button, classNames);
            }

            void Leave(PointerLeaveEvent _) => HubColors.ApplyButton(button, classNames);

            Register(button, Enter, Leave);
        }

        public static void BindTab(Button tab)
        {
            if (tab == null) return;
            Unbind(tab);

            void Enter(PointerEnterEvent _)
            {
                tab.style.backgroundColor = HubColors.TabBg;
                tab.style.color = tab.ClassListContains("hub-tab--active")
                    ? HubColors.TabActive
                    : Rgb(200, 208, 220);
            }

            void Leave(PointerLeaveEvent _) =>
                HubColors.ApplyTab(tab, tab.ClassListContains("hub-tab--active"));

            Register(tab, Enter, Leave);
        }

        public static void BindModeToggle(Button button)
        {
            if (button == null) return;
            Unbind(button);

            void Enter(PointerEnterEvent _)
            {
                button.style.backgroundColor = HubColors.TabBg;
                button.style.color = button.ClassListContains("hub-tab--active")
                    ? HubColors.TabActive
                    : Rgb(200, 208, 220);
            }

            void Leave(PointerLeaveEvent _) =>
                HubColors.ApplyModeToggle(button, button.ClassListContains("hub-tab--active"));

            Register(button, Enter, Leave);
        }

        public static void BindDepLink(Button button)
        {
            if (button == null) return;
            Unbind(button);

            void Enter(PointerEnterEvent _) => button.style.color = Rgb(120, 170, 255);
            void Leave(PointerLeaveEvent _) => HubColors.ApplyDepLinkButton(button);

            Register(button, Enter, Leave);
        }

        public static void BindListItem(VisualElement item)
        {
            if (item == null) return;
            Unbind(item);

            void Enter(PointerEnterEvent _) =>
                item.style.backgroundColor = Rgb(40, 44, 56);

            void Leave(PointerLeaveEvent _) =>
                HubColors.ApplyListItem(item, item.ClassListContains("hub-list-item--selected"));

            Register(item, Enter, Leave);
        }

        private static void ApplyButtonHover(Button button, string classNames)
        {
            if (!button.enabledSelf) return;

            if (classNames != null && classNames.Contains("hub-btn--primary"))
            {
                button.style.backgroundColor = Rgb(100, 155, 255);
                return;
            }

            if (classNames != null && classNames.Contains("hub-btn--accent"))
            {
                button.style.backgroundColor = Rgb(62, 145, 110);
                return;
            }

            if (classNames != null && classNames.Contains("hub-btn--danger"))
            {
                button.style.color = Rgb(245, 140, 140);
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = Rgb(220, 100, 100);
                return;
            }

            button.style.backgroundColor = Rgb(50, 54, 68);
        }

        private static void Register(
            VisualElement element,
            EventCallback<PointerEnterEvent> enter,
            EventCallback<PointerLeaveEvent> leave)
        {
            element.RegisterCallback(enter);
            element.RegisterCallback(leave);
            element.userData = new Binding { Enter = enter, Leave = leave };
        }

        private static void Unbind(VisualElement element)
        {
            if (element?.userData is not Binding binding)
                return;

            element.UnregisterCallback(binding.Enter);
            element.UnregisterCallback(binding.Leave);
            element.userData = null;
        }

        private static Color Rgb(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f);
    }
}
#endif
