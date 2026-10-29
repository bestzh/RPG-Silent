using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "RPG/Dungeon Variation")]
public sealed class DungeonVariationDefinition : ScriptableObject
{
    public int DungeonId;
    public EnemyController Melee, Ranged;
    public Vector3[] Route, AmbushPoints;
    // Bit 0 supply chest, bit 1 optional elite. Gates and their dependent treasures never enter this list.
    public string[] OptionalNodeIds;
    public Material MarkerMaterial;
    public int AgentType => Melee.GetComponent<NavMeshAgent>().agentTypeID;

    public bool ValidateRoute(DungeonDatabase.Entry dungeon, ExplorationNode[] nodes, out string error)
    {
        error = "远征路线配置缺失，请返回主城。";
        if (dungeon == null || DungeonId != dungeon.Id || Melee == null || Ranged == null || MarkerMaterial == null ||
            Route == null || Route.Length < 4 || AmbushPoints == null || AmbushPoints.Length != 2 || OptionalNodeIds == null || OptionalNodeIds.Length != 2 ||
            Melee.GetComponent<NavMeshAgent>() == null || Ranged.GetComponent<NavMeshAgent>() == null || Ranged.GetComponent<NavMeshAgent>().agentTypeID != AgentType ||
            Melee.GetComponent<BossPhaseController>() != null || Ranged.GetComponent<BossPhaseController>() != null) return false;
        for (int i = 0; i < OptionalNodeIds.Length; i++)
        {
            var matching = nodes.Where(n => n.PersistentId == OptionalNodeIds[i]).ToArray();
            if (matching.Length != 1 || matching[0].Kind != (i == 0 ? ExplorationKind.Chest : ExplorationKind.EliteEvent) ||
                !matching[0].gameObject.activeInHierarchy || matching[0].MovingGate != null || matching[0].RevealRoot != null) return false;
        }
        var filter = new NavMeshQueryFilter { agentTypeID = AgentType, areaMask = NavMesh.AllAreas };
        var path = new NavMeshPath();
        foreach (var point in Route.Concat(AmbushPoints))
        {
            if (!Finite(point) || !NavMesh.SamplePosition(point, out var hit, .5f, filter) || Vector3.Distance(hit.position, point) > .5f ||
                !NavMesh.CalculatePath(Route[0], hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
            { error = "远征路线或伏击点不可达，本次未启动。请返回主城检查导航。"; return false; }
        }
        if (!NavMesh.SamplePosition(dungeon.SpawnPosition, out var entrance, 2, filter) ||
            !NavMesh.CalculatePath(entrance.position, Route[0], filter, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        for (int i = 0; i < Route.Length; i++)
            if (!NavMesh.CalculatePath(Route[i], Route[(i + 1) % Route.Length], filter, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        error = null; return true;
    }
    static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
}
