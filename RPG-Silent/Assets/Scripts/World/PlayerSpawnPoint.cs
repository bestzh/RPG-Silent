using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家出生点：场景加载完成后，把玩家放到出生位置。
/// 出生点优先从 DungeonDatabase 表中按当前进入的副本 ID 读取（DungeonLaunchContext）；
/// 若无表数据（例如直接在编辑器里运行该场景），则退回使用本对象自身的 Transform。
/// 玩家使用 Rigidbody，传送时会清零速度，避免被物理惯性带走。
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("副本表（留空则自动从 Resources 加载 DungeonDatabase）")]
    [SerializeField] private DungeonDatabase dungeonDatabase;

    [Tooltip("玩家对象的 Tag")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("是否对齐出生点朝向")]
    [SerializeField] private bool alignRotation = true;

    [Tooltip("延迟到下一物理帧再定位，避免与玩家自身的初始化/物理写入冲突")]
    [SerializeField] private bool waitForFixedUpdate = true;

    private IEnumerator Start()
    {
        if (waitForFixedUpdate)
            yield return new WaitForFixedUpdate();

        GameObject player = GameObject.FindWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning(
                $"[PlayerSpawnPoint] 未找到 Tag={playerTag} 的玩家，无法定位。" +
                "请确认场景中存在玩家对象。", this);
            yield break;
        }

        ResolveSpawn(out Vector3 pos, out Quaternion rot, out string source);
        PlacePlayer(player.transform, pos, rot);
        Debug.Log($"[PlayerSpawnPoint] 已将玩家放置到出生点（来源：{source}）。");
    }

    private void ResolveSpawn(out Vector3 pos, out Quaternion rot, out string source)
    {
        if (dungeonDatabase == null)
            dungeonDatabase = Resources.Load<DungeonDatabase>("DungeonDatabase");

        if (DungeonLaunchContext.HasValue &&
            dungeonDatabase != null &&
            dungeonDatabase.TryGetById(DungeonLaunchContext.DungeonId, out DungeonDatabase.Entry entry))
        {
            pos = entry.SpawnPosition;
            rot = alignRotation ? entry.SpawnRotation : Quaternion.identity;
            source = $"表配置 副本ID={entry.Id}";
            Debug.Log($"[PlayerSpawnPoint] 找到表配置，使用表配置：{source}");
            Debug.Log($"表配置：{pos} {rot}");
            Debug.Log($"场景对象 Transform：{transform.position} {transform.rotation}");
            return;
        }

        // 兜底：使用本对象自身的 Transform
        pos = transform.position;
        rot = transform.rotation;
        source = "场景对象 Transform";
        Debug.Log($"[PlayerSpawnPoint] 未找到表配置，使用场景对象 Transform：{source}");
        Debug.Log($"场景对象 Transform：{transform.position} {transform.rotation}");
    }

    private void PlacePlayer(Transform player, Vector3 pos, Quaternion rot)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 运动学刚体不支持写速度，写了会报警告；仅对非运动学刚体清零速度。
            if (!rb.isKinematic)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.position        = pos;
            if (alignRotation) rb.rotation = rot;
        }

        if (alignRotation)
            player.SetPositionAndRotation(pos, rot);
        else
            player.position = pos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
    }
}
