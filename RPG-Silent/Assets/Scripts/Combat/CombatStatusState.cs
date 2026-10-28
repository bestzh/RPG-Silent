using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One entry per status kind. Refresh never duplicates stacks or resets the damage tick.</summary>
public sealed class CombatStatusState
{
    public sealed class Entry
    {
        public CombatStatusDefinition Definition { get; internal set; }
        public float Remaining { get; internal set; }
        public int Magnitude { get; internal set; }
        internal float TickElapsed;
    }
    readonly List<Entry> entries = new();
    public IReadOnlyList<Entry> Entries => entries;
    public bool HasDebuffs => entries.Any(e => e.Definition.IsDebuff);
    public float MovementMultiplier => 1f - (entries.FirstOrDefault(e => e.Definition.Kind == CombatStatusKind.Chilled)?.Magnitude ?? 0) / 100f;
    public float IncomingMultiplier => 1f - (entries.FirstOrDefault(e => e.Definition.Kind == CombatStatusKind.Guard)?.Magnitude ?? 0) / 100f;
    public bool Apply(CombatStatusDefinition definition)
    {
        if (definition == null || !Enum.IsDefined(typeof(CombatStatusKind), definition.Kind) || float.IsNaN(definition.Duration) ||
            float.IsInfinity(definition.Duration) || definition.Duration <= 0 || definition.Duration > 120 || definition.Magnitude < 1 || definition.Magnitude > 80) return false;
        var current = entries.FirstOrDefault(e => e.Definition.Kind == definition.Kind);
        if (current == null) entries.Add(new Entry { Definition = definition, Remaining = definition.Duration, Magnitude = definition.Magnitude });
        else { current.Remaining = Math.Max(current.Remaining, definition.Duration); if (definition.Magnitude >= current.Magnitude) { current.Definition = definition; current.Magnitude = definition.Magnitude; } }
        return true;
    }
    public int Tick(float delta)
    {
        if (float.IsNaN(delta) || float.IsInfinity(delta) || delta <= 0) return 0;
        int damage = 0;
        foreach (var entry in entries)
        {
            float elapsed = Math.Min(delta, entry.Remaining);
            if (entry.Definition.Kind == CombatStatusKind.Burning)
            {
                entry.TickElapsed += elapsed;
                int ticks = (int)Math.Floor(entry.TickElapsed + .00001f);
                damage += ticks * entry.Magnitude; entry.TickElapsed -= ticks;
            }
            entry.Remaining = Math.Max(0, entry.Remaining - elapsed);
        }
        entries.RemoveAll(e => e.Remaining <= 0); return damage;
    }
    public void Cleanse() => entries.RemoveAll(e => e.Definition.IsDebuff);
    public void Clear() => entries.Clear();
}
