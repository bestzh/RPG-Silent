using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns pooled instances of an authored row prefab, not an independent gameplay model.</summary>
[DisallowMultipleComponent]
public sealed class UIListView : MonoBehaviour
{
    [SerializeField] private UIListRowView rowPrefab;
    private readonly List<UIListRowView> rows = new();
    private int used;
    public int VisibleCount => used;

    public void Begin()
    {
        used = 0;
        foreach (var row in rows) row.gameObject.SetActive(false);
    }

    public UIListRowView Add(string title, string body = "", Sprite icon = null)
    {
        if (used == rows.Count)
        {
            var row = Instantiate(rowPrefab, transform, false);
            row.name = "Row_" + used.ToString("D2");
            rows.Add(row);
        }
        var next = rows[used++];
        next.gameObject.SetActive(true);
        next.transform.SetAsLastSibling();
        next.Present(title, body, icon);
        return next;
    }
}
