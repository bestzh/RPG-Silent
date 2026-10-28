using System.Collections.Generic;
using UnityEngine;

public enum TownNpcRole { Mentor, Blacksmith, Steward, Scout, Resident }
public sealed class TownNpc : MonoBehaviour
{
    public static readonly HashSet<TownNpc> Active = new();
    public string DisplayName;
    public TownNpcRole Role;
    public PlayerClassDefinition Profession;
    [TextArea] public string Greeting;
    public string QuestMarker { get; internal set; } = "";
    public string MapSymbol => Role switch { TownNpcRole.Mentor => "师", TownNpcRole.Blacksmith => "匠", TownNpcRole.Steward => "主", TownNpcRole.Scout => "探", _ => "民" };
    [Min(1f)] public float InteractionRadius = 2.8f;
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetActive() => Active.Clear();
}
