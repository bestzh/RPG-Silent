using System;
using UnityEngine;
[Serializable] public sealed class SecretCatalogEntry { public string Id, Label; }
[CreateAssetMenu(menuName = "RPG/Adventure Codex")]
public sealed class AdventureCatalog : ScriptableObject
{
    public EnemyDefinition[] Enemies = Array.Empty<EnemyDefinition>();
    public SecretCatalogEntry[] Secrets = Array.Empty<SecretCatalogEntry>();
}
