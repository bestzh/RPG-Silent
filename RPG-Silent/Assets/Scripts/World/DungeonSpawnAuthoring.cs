using UnityEngine;

/// <summary>
/// 出生点摆放工具（仅用于编辑器中可视化配置）：
/// 在副本场景里放一个空物体挂上本组件，选择副本表与副本 ID，
/// 然后在 Scene 视图用手柄拖动本物体，即可实时把出生点位置/朝向写回 DungeonDatabase。
/// 运行时本组件不做任何事，仅作为编辑期的“摆位”辅助。
/// </summary>
public class DungeonSpawnAuthoring : MonoBehaviour
{
    [Tooltip("要写入的副本表")]
    public DungeonDatabase database;

    [Tooltip("要配置出生点的副本 ID")]
    public int dungeonId;

    [Tooltip("Gizmo 半径")]
    public float gizmoRadius = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 1f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * gizmoRadius * 3f);
    }
}
