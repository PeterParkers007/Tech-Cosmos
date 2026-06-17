#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    internal static class HubUiFactory
    {
        public static Label Label(string text, string className = null)
        {
            var l = new Label(text) { text = text };
            if (!string.IsNullOrEmpty(className)) l.AddToClassList(className);
            return l;
        }

        public static Button Button(string text, string className, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            if (!string.IsNullOrEmpty(className)) b.AddToClassList(className);
            return b;
        }

        public static VisualElement StatusDot(PackagePresence presence)
        {
            var dot = new VisualElement();
            dot.AddToClassList("hub-status-dot");
            dot.AddToClassList(presence switch
            {
                PackagePresence.InManifest => "hub-status-dot--installed",
                PackagePresence.LocalOnly => "hub-status-dot--local",
                _ => "hub-status-dot--missing"
            });
            return dot;
        }

        public static Label Badge(string text, string className)
        {
            var b = Label(text, "hub-badge " + className);
            return b;
        }

        public static VisualElement CheckItem(bool ok, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-check-item");

            var icon = Label(ok ? "✓" : "✗", ok ? "hub-check-icon hub-check-icon--ok" : "hub-check-icon hub-check-icon--fail");
            var label = Label(text, "hub-check-label");
            row.Add(icon);
            row.Add(label);
            return row;
        }

        public static VisualElement Field(string label, string value, Action<string> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-field-row");
            row.Add(Label(label, "hub-field-label"));

            var field = new TextField { value = value };
            field.AddToClassList("hub-field-input");
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            row.Add(field);
            return row;
        }

        public static VisualElement ToggleRow(string label, bool value, Action<bool> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("hub-field-row");

            var toggle = new Toggle(label) { value = value };
            toggle.AddToClassList("hub-toggle");
            toggle.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            row.Add(toggle);
            return row;
        }
    }
}
#endif
