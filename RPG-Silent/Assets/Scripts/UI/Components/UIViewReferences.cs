using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Serialized prefab wiring. Names are stable authoring IDs; targets are direct object references.</summary>
[DisallowMultipleComponent]
public sealed class UIViewReferences : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        public Transform Scope;
        public string Id;
        public GameObject Target;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    private Dictionary<Transform, Dictionary<string, GameObject>> lookup;
    public IReadOnlyList<Entry> Entries => entries;

    private void BuildLookup()
    {
        if (lookup != null) return;
        lookup = new Dictionary<Transform, Dictionary<string, GameObject>>();
        foreach (var entry in entries)
        {
            if (entry.Scope == null || entry.Target == null || string.IsNullOrEmpty(entry.Id)) continue;
            if (!lookup.TryGetValue(entry.Scope, out var local))
                lookup.Add(entry.Scope, local = new Dictionary<string, GameObject>(StringComparer.Ordinal));
            if (!local.TryAdd(entry.Id, entry.Target))
                throw new InvalidOperationException($"Duplicate UI binding '{entry.Id}' on {entry.Scope.name}.");
        }
    }

    public Transform Find(Transform scope, string path)
    {
        BuildLookup();
        var current = scope;
        foreach (var id in path.Split('/'))
        {
            if (string.IsNullOrEmpty(id)) continue;
            if (!lookup.TryGetValue(current, out var local) || !local.TryGetValue(id, out var target) || target == null)
                return null;
            current = target.transform;
        }
        return current;
    }

#if UNITY_EDITOR
    public void SetAuthoringEntries(Entry[] value) { entries = value; lookup = null; }
#endif
}

/// <summary>Bind and present authored controls. This class never creates or resizes a UI object.</summary>
public static class UIView
{
    public static Transform Find(Transform scope, string path)
    {
        if (scope == null) return null;
        var references = scope.GetComponentInParent<UIViewReferences>(true);
        return references != null ? references.Find(scope, path) : null;
    }

    public static T Require<T>(Transform scope, string id) where T : Component
    {
        var target = Find(scope, id);
        var result = target != null ? target.GetComponent<T>() : null;
        if (result == null)
            throw new InvalidOperationException($"UI prefab binding '{id}' requires {typeof(T).Name} under '{scope?.name}'. Re-author the prefab before opening this view.");
        return result;
    }

    public static RectTransform Rect(Transform scope, string id) => Require<RectTransform>(scope, id);
    public static RectTransform Panel(Transform scope, string id) => Require<RectTransform>(scope, id);
    public static TMP_Text Text(Transform scope, string id, string text)
    {
        var label = Require<TMP_Text>(scope, id);
        return label;
    }

    public static Button Button(Transform scope, string id, string label, UnityAction action)
    {
        var button = Require<Button>(scope, id);
        if (action != null) button.onClick.AddListener(action);
        return button;
    }
}
