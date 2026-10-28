using RPGSilent.Domain;
using UnityEngine;

public sealed class CombatStatusController : MonoBehaviour
{
    public CombatStatusState State { get; } = new();
    IDamageable target;
    PlayerController player;
    EnemyController enemy;
    void Awake() { target = GetComponent<IDamageable>(); player = GetComponent<PlayerController>(); enemy = GetComponent<EnemyController>(); }
    public bool Apply(CombatStatusDefinition definition)
    {
        if (target == null || target.IsDead || (player != null && !player.CanReceiveCombatEffects) || (enemy != null && enemy.CombatLocked)) return false;
        return State.Apply(definition);
    }
    void Update()
    {
        if (target == null || target.IsDead || (player != null && !player.CanReceiveCombatEffects) || (enemy != null && enemy.CombatLocked)) { State.Clear(); return; }
        int damage = State.Tick(Time.deltaTime);
        if (damage > 0) { if (player != null) player.TakePeriodicDamage(damage); else if (enemy != null) enemy.TakePeriodicDamage(damage); else target.TakeDamage(damage); }
    }
    void OnDisable() => State.Clear();
}
