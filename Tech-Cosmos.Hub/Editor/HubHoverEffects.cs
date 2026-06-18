#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// C# 悬停/按下反馈：inline 样式会盖掉 USS 的 :hover/:active，用 Manipulator + 指针事件模拟 Web 交互。
    /// </summary>
    internal static class HubHoverEffects
    {
        private sealed class PressFeedbackManipulator : Manipulator
        {
            private readonly Func<bool> _canPress;
            private readonly Action _applyNormal;
            private readonly Action _applyHover;
            private readonly Action _applyPressed;
            private bool _hovered;
            private bool _pressed;

            public PressFeedbackManipulator(
                Func<bool> canPress,
                Action applyNormal,
                Action applyHover,
                Action applyPressed)
            {
                _canPress = canPress;
                _applyNormal = applyNormal;
                _applyHover = applyHover;
                _applyPressed = applyPressed;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerEnterEvent>(OnEnter);
                target.RegisterCallback<PointerLeaveEvent>(OnLeave);
                target.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
                target.RegisterCallback<PointerDownEvent>(OnDown);
                target.RegisterCallback<PointerUpEvent>(OnUp);
                target.RegisterCallback<PointerCancelEvent>(OnUp);
                target.RegisterCallback<PointerCaptureOutEvent>(OnUp);
                target.RegisterCallback<ClickEvent>(OnUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<PointerEnterEvent>(OnEnter);
                target.UnregisterCallback<PointerLeaveEvent>(OnLeave);
                target.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
                target.UnregisterCallback<PointerDownEvent>(OnDown);
                target.UnregisterCallback<PointerUpEvent>(OnUp);
                target.UnregisterCallback<PointerCancelEvent>(OnUp);
                target.UnregisterCallback<PointerCaptureOutEvent>(OnUp);
                target.UnregisterCallback<ClickEvent>(OnUp);
            }

            private void OnEnter(PointerEnterEvent _) => SetHovered(true);

            private void OnLeave(PointerLeaveEvent _) => SetHovered(false);

            private void OnDown(PointerDownEvent _)
            {
                if (_canPress != null && !_canPress()) return;
                if (_pressed) return;
                _pressed = true;
                _applyPressed?.Invoke();
            }

            private void OnUp(EventBase _)
            {
                if (!_pressed) return;
                _pressed = false;
                Refresh();
            }

            private void SetHovered(bool hovered)
            {
                _hovered = hovered;
                if (_pressed) return;
                Refresh();
            }

            public void ResetVisual()
            {
                _pressed = false;
                Refresh();
            }

            private void Refresh()
            {
                if (_canPress != null && !_canPress())
                {
                    _applyNormal?.Invoke();
                    return;
                }

                if (_pressed)
                    _applyPressed?.Invoke();
                else if (_hovered)
                    _applyHover?.Invoke();
                else
                    _applyNormal?.Invoke();
            }
        }

        public static void BindButton(Button button, string classNames)
        {
            if (button == null) return;
            PrepareButton(button);
            var manipulator = AttachPressFeedback(button, new PressFeedbackManipulator(
                () => button.enabledSelf,
                () =>
                {
                    ClearPressMotion(button);
                    HubColors.ApplyButton(button, classNames);
                },
                () => ApplyButtonHover(button, classNames),
                () => ApplyButtonPressed(button, classNames)));
            manipulator.ResetVisual();
        }

        public static void BindTab(Button tab)
        {
            if (tab == null) return;
            PrepareButton(tab);
            var manipulator = AttachPressFeedback(tab, new PressFeedbackManipulator(
                () => true,
                () =>
                {
                    ClearPressMotion(tab);
                    HubColors.ApplyTab(tab, tab.ClassListContains("hub-tab--active"));
                },
                () => ApplyTabHover(tab),
                () => ApplyTabPressed(tab)));
            manipulator.ResetVisual();
        }

        public static void BindModeToggle(Button button)
        {
            if (button == null) return;
            PrepareButton(button);
            var manipulator = AttachPressFeedback(button, new PressFeedbackManipulator(
                () => true,
                () =>
                {
                    ClearPressMotion(button);
                    HubColors.ApplyModeToggle(button, button.ClassListContains("hub-tab--active"));
                },
                () => ApplyModeToggleHover(button),
                () => ApplyModeTogglePressed(button)));
            manipulator.ResetVisual();
        }

        public static void BindDepLink(Button button)
        {
            if (button == null) return;
            PrepareButton(button);
            var manipulator = AttachPressFeedback(button, new PressFeedbackManipulator(
                () => true,
                () =>
                {
                    ClearPressMotion(button);
                    HubColors.ApplyDepLinkButton(button);
                },
                () => ApplyDepLinkHover(button),
                () => ApplyDepLinkPressed(button)));
            manipulator.ResetVisual();
        }

        public static void BindListItem(VisualElement item)
        {
            if (item == null) return;
            item.pickingMode = PickingMode.Position;
            var manipulator = AttachPressFeedback(item, new PressFeedbackManipulator(
                () => true,
                () =>
                {
                    ClearPressMotion(item);
                    HubColors.ApplyListItem(item, item.ClassListContains("hub-list-item--selected"));
                },
                () => ApplyListItemHover(item),
                () => ApplyListItemPressed(item)));
            manipulator.ResetVisual();
        }

        public static void BindToggle(Toggle toggle)
        {
            if (toggle == null) return;
            toggle.pickingMode = PickingMode.Position;
            var manipulator = AttachPressFeedback(toggle, new PressFeedbackManipulator(
                () => toggle.enabledSelf,
                () =>
                {
                    ClearPressMotion(toggle);
                    HubColors.ApplyToggle(toggle);
                },
                () => ApplyToggleHover(toggle),
                () => ApplyTogglePressed(toggle)));
            manipulator.ResetVisual();
        }

        public static void BindGitVersionTrigger(Button trigger)
        {
            if (trigger == null) return;
            PrepareButton(trigger);
            var manipulator = AttachPressFeedback(trigger, new PressFeedbackManipulator(
                () => trigger.enabledSelf,
                () =>
                {
                    ClearPressMotion(trigger);
                    HubColors.ApplyGitVersionTrigger(trigger);
                },
                () => ApplyGitVersionTriggerHover(trigger),
                () => ApplyGitVersionTriggerPressed(trigger)));
            manipulator.ResetVisual();
        }

        public static void BindMarkdownLink(Label label)
        {
            if (label == null) return;
            label.pickingMode = PickingMode.Position;
            var manipulator = AttachPressFeedback(label, new PressFeedbackManipulator(
                () => true,
                () =>
                {
                    ClearPressMotion(label);
                    HubColors.ApplyMarkdownLink(label);
                },
                () => ApplyMarkdownLinkHover(label),
                () => ApplyMarkdownLinkPressed(label)));
            manipulator.ResetVisual();
        }

        private static PressFeedbackManipulator AttachPressFeedback(
            VisualElement element,
            PressFeedbackManipulator manipulator)
        {
            if (element.userData is PressFeedbackManipulator existing)
            {
                element.RemoveManipulator(existing);
                element.userData = null;
            }

            element.userData = manipulator;
            element.AddManipulator(manipulator);
            return manipulator;
        }

        private static void PrepareButton(Button button)
        {
            button.pickingMode = PickingMode.Position;
            button.focusable = true;
        }

        private static void ApplyButtonHover(Button button, string classNames)
        {
            if (!button.enabledSelf) return;
            ClearPressMotion(button);

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
                button.style.backgroundColor = Rgb(55, 28, 28);
                button.style.color = Rgb(245, 140, 140);
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = Rgb(220, 100, 100);
                return;
            }

            button.style.backgroundColor = Rgb(50, 54, 68);
        }

        private static void ApplyButtonPressed(Button button, string classNames)
        {
            if (!button.enabledSelf) return;
            SetPressMotion(button);

            if (classNames != null && classNames.Contains("hub-btn--primary"))
            {
                button.style.backgroundColor = Rgb(52, 98, 210);
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = Rgb(42, 82, 180);
                return;
            }

            if (classNames != null && classNames.Contains("hub-btn--accent"))
            {
                button.style.backgroundColor = Rgb(36, 92, 68);
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = Rgb(30, 78, 58);
                return;
            }

            if (classNames != null && classNames.Contains("hub-btn--danger"))
            {
                button.style.backgroundColor = Rgb(42, 18, 18);
                button.style.color = Rgb(220, 100, 100);
                button.style.borderTopColor = button.style.borderRightColor =
                    button.style.borderBottomColor = button.style.borderLeftColor = Rgb(170, 60, 60);
                return;
            }

            button.style.backgroundColor = Rgb(34, 38, 50);
            button.style.borderTopColor = button.style.borderRightColor =
                button.style.borderBottomColor = button.style.borderLeftColor = Rgb(45, 50, 64);
        }

        private static void ApplyTabHover(Button tab)
        {
            ClearPressMotion(tab);
            tab.style.backgroundColor = HubColors.TabBg;
            tab.style.color = tab.ClassListContains("hub-tab--active")
                ? HubColors.TabActive
                : Rgb(200, 208, 220);
        }

        private static void ApplyTabPressed(Button tab)
        {
            SetPressMotion(tab);
            tab.style.backgroundColor = Rgb(28, 32, 44);
            tab.style.color = tab.ClassListContains("hub-tab--active")
                ? Rgb(80, 145, 235)
                : Rgb(170, 178, 195);
        }

        private static void ApplyModeToggleHover(Button button)
        {
            ClearPressMotion(button);
            button.style.backgroundColor = HubColors.TabBg;
            button.style.color = button.ClassListContains("hub-tab--active")
                ? HubColors.TabActive
                : Rgb(200, 208, 220);
        }

        private static void ApplyModeTogglePressed(Button button)
        {
            SetPressMotion(button);
            button.style.backgroundColor = Rgb(28, 32, 44);
            button.style.color = button.ClassListContains("hub-tab--active")
                ? Rgb(80, 145, 235)
                : Rgb(170, 178, 195);
        }

        private static void ApplyDepLinkHover(Button button)
        {
            ClearPressMotion(button);
            button.style.color = Rgb(120, 170, 255);
            button.style.backgroundColor = new Color(0, 0, 0, 0);
        }

        private static void ApplyDepLinkPressed(Button button)
        {
            SetPressMotion(button);
            button.style.color = Rgb(90, 135, 220);
            button.style.backgroundColor = Rgb(28, 34, 48);
        }

        private static void ApplyGitVersionTriggerHover(Button trigger)
        {
            ClearPressMotion(trigger);
            HubColors.ApplyGitVersionTriggerHover(trigger);
        }

        private static void ApplyGitVersionTriggerPressed(Button trigger)
        {
            HubColors.ApplyGitVersionTriggerPressed(trigger);
            SetPressMotion(trigger);
        }

        private static void ApplyListItemHover(VisualElement item)
        {
            item.style.backgroundColor = Rgb(40, 44, 56);
        }

        private static void ApplyListItemPressed(VisualElement item)
        {
            item.style.backgroundColor = Rgb(32, 36, 48);
            SetPressMotion(item);
        }

        private static void ApplyToggleHover(Toggle toggle)
        {
            ClearPressMotion(toggle);
            toggle.style.backgroundColor = Rgb(40, 44, 56);
        }

        private static void ApplyTogglePressed(Toggle toggle)
        {
            SetPressMotion(toggle);
            toggle.style.backgroundColor = Rgb(32, 36, 48);
        }

        private static void ApplyMarkdownLinkHover(Label label)
        {
            ClearPressMotion(label);
            label.style.color = Rgb(120, 170, 255);
            label.style.backgroundColor = new Color(79f / 255f, 140f / 255f, 255f / 255f, 0.08f);
        }

        private static void ApplyMarkdownLinkPressed(Label label)
        {
            SetPressMotion(label);
            label.style.color = Rgb(90, 135, 220);
            label.style.backgroundColor = Rgb(28, 34, 48);
        }

        private static void SetPressMotion(VisualElement element)
        {
            element.style.translate = new Translate(0, 1);
            element.style.scale = new Scale(new Vector2(0.985f, 0.985f));
        }

        private static void ClearPressMotion(VisualElement element)
        {
            element.style.translate = new Translate(0, 0);
            element.style.scale = new Scale(Vector2.one);
        }

        private static Color Rgb(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f);
    }
}
#endif
