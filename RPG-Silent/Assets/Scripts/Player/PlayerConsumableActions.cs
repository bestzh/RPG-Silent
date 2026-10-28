using RPGSilent.Domain;
using UnityEngine;
using VContainer;

public sealed class PlayerConsumableActions : MonoBehaviour
{
    [Inject] IPlayerInputActions input;
    [Inject] PlayerProgressService progress;
    public string Message { get; private set; } = string.Empty;
    public float MessageUntil { get; private set; }
    void OnEnable() { if (input != null) input.ConsumableTriggered += UseSlot; }
    void OnDisable() { if (input != null) input.ConsumableTriggered -= UseSlot; }
    public void UseSlot(int slot)
    {
        progress.TryUseConsumableSlot(slot, out string message); Message = message; MessageUntil = Time.unscaledTime + 3;
    }
}
