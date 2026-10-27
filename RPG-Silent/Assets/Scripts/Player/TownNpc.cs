using System.Collections.Generic;
using UnityEngine;

public enum TownNpcRole { Mentor, Blacksmith }
public sealed class TownNpc : MonoBehaviour
{
    public static readonly HashSet<TownNpc> Active = new();
    public string DisplayName;
    public TownNpcRole Role;
    public PlayerClassDefinition Profession;
    [Min(1f)] public float InteractionRadius = 2.8f;
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);
}
