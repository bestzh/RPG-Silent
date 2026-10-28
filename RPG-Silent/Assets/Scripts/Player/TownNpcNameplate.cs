using UnityEngine;
using TMPro;
using VContainer;

public sealed class TownNpcNameplate : MonoBehaviour
{
    Camera view;
    TownNpc npc;
    TMP_Text label;
    PlayerProgressService progress;
    void Start()
    {
        npc = GetComponentInParent<TownNpc>(); label = GetComponent<TMP_Text>();
        if (npc == null || label == null) return;
        var container = GameLifetimeScope.Instance?.Container;
        if (container == null) return;
        progress = container.Resolve<PlayerProgressService>(); progress.Changed += Refresh;
        Refresh();
    }
    void Refresh()
    {
        if (npc == null || label == null || progress == null) return;
        npc.QuestMarker = progress.QuestMarkerFor(npc);
        string marker = npc.QuestMarker == "?" ? "<color=#8FFFD2>? 可提交任务</color>\n" : npc.QuestMarker == "!" ? "<color=#FFD568>! 可接任务</color>\n" : "";
        label.text = marker + npc.DisplayName;
    }
    void OnDestroy() { if (progress != null) progress.Changed -= Refresh; }
    void LateUpdate()
    {
        if (view == null) view = Camera.main;
        if (view != null) transform.rotation = view.transform.rotation;
        if (label != null && view != null) label.enabled = Vector3.Distance(view.transform.position, transform.position) < 24;
    }
}
