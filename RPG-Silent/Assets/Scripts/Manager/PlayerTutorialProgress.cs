using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    TutorialData tutorial = new();
    public TutorialData Tutorial => tutorial.Copy();
    public bool ShouldTrackTutorialMovement => attached && tutorial.enabled && !tutorial.Has(TutorialStep.Move);

    // Transient gameplay actions have already happened. A failed write retains the
    // bit in memory; the normal save retry includes it without repeating rewards.
    public void RecordTutorialAction(TutorialStep step)
    {
        if (!attached || applying || stats.IsDead || !tutorial.Record(step)) return;
        Save();
    }
    public bool TrySetTutorialEnabled(bool enabled, out string message)
    {
        message = "角色尚未就绪，或当前无法操作引导。";
        if (!attached || applying || stats.IsDead) return false;
        var data = Capture(); data.tutorial.enabled = enabled;
        if (!store.TrySave(data, newProfile, out message))
        { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        tutorial = data.tutorial.Copy(); world = data.world.Copy();
        newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false;
        message = StatusMessage = enabled ? (tutorial.IsComplete ? "引导已完成，可随时回看。" : "已继续未完成的引导。") : "已跳过引导，可从引导回看中继续。";
        Changed?.Invoke(); return true;
    }
}
