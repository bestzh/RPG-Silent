using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "RPG/Long Challenge Arena")]
public sealed class LongChallengeArena : ScriptableObject
{
    public int DungeonId;
    public EnemyController Melee, Ranged;
    public EnemyController[] Bosses;
    public Vector3 Center, PlayerSpawn, BossSpawn;
    public Vector3[] GroupSpawns;
    public float Radius = 22;
    public int AgentType => Melee.GetComponent<NavMeshAgent>().agentTypeID;
    public bool ValidateArena(out string error)
    {
        error = "挑战区域配置缺失。";
        if ((DungeonId != 1001 && DungeonId != 1002) || Melee == null || Ranged == null || Bosses == null || Bosses.Length != 2 || Bosses.Any(b => b == null || b.GetComponent<BossPhaseController>() == null) ||
            GroupSpawns == null || GroupSpawns.Length != 3 || Radius < 12 || Radius > 30 || float.IsNaN(Radius) ||
            new[] { Melee, Ranged }.Concat(Bosses).Any(p => p.Definition == null || p.GetComponent<NavMeshAgent>() == null) ||
            new[] { Ranged }.Concat(Bosses).Any(p => p.GetComponent<NavMeshAgent>().agentTypeID != AgentType)) return false;
        var filter = new NavMeshQueryFilter { agentTypeID = AgentType, areaMask = NavMesh.AllAreas }; var path = new NavMeshPath();
        foreach (var point in GroupSpawns.Concat(new[] { Center, PlayerSpawn, BossSpawn }))
        {
            if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z) || float.IsInfinity(point.x) || float.IsInfinity(point.y) || float.IsInfinity(point.z) ||
                Vector3.Distance(point, Center) > Radius - 5 || !NavMesh.SamplePosition(point, out var nav, .5f, filter) || Vector3.Distance(point, nav.position) > .5f ||
                !NavMesh.CalculatePath(PlayerSpawn, point, filter, path) || path.status != NavMeshPathStatus.PathComplete)
            { error = "挑战出生点或玩家位置缺少完整导航路径，请返回主城。"; return false; }
        }
        if (Vector3.Distance(PlayerSpawn, BossSpawn) < 4 || GroupSpawns.Any(p => Vector3.Distance(p, PlayerSpawn) < 3))
        { error = "挑战双方出生点过近。"; return false; }
        error = null; return true;
    }
}
