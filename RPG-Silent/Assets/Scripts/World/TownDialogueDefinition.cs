using System;
using UnityEngine;

[Serializable] public sealed class TownDialogueChoice
{
    public string Id, Label;
    [TextArea] public string Outcome;
    public int Gold, Experience, Affinity;
}
[CreateAssetMenu(menuName = "RPG/Town Dialogue")]
public sealed class TownDialogueDefinition : ScriptableObject
{
    public string Id, Title;
    public TownNpcRole Role;
    public string RequiredQuestId = string.Empty;
    [Range(1, 50)] public int RequiredLevel = 1;
    [Range(0, 100)] public int RequiredAffinity;
    public int Order;
    [TextArea(2, 6)] public string[] Pages = Array.Empty<string>();
    public TownDialogueChoice[] Choices = Array.Empty<TownDialogueChoice>();
}
