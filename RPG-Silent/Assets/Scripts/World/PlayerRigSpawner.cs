using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 玩家 rig 生成器：放在每个游戏场景里，保证场景中存在唯一、跨场景保留的玩家，
/// 并在通过传送门进入副本时，按表把玩家定位到出生点。
///
/// 工作方式：
/// - Awake：若当前已存在玩家（从别的场景保留过来的）则不再生成；否则实例化共用的玩家
///   rig 预制体，用全局容器注入依赖后再激活，并 DontDestroyOnLoad。
/// - Start：若存在副本启动上下文（DungeonLaunchContext）且当前场景与该副本配置的场景一致，
///   则从 DungeonDatabase 读取出生点，把（可能是跨场景保留下来的）玩家放过去。
///
/// 采用「先非激活实例化 → 注入 → 再激活」的顺序，确保 PlayerController.OnEnable
/// 运行前依赖已注入（否则输入事件订阅会因依赖为空而跳过）。
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerRigSpawner : MonoBehaviour
{
    [Header("玩家 rig")]
    [Tooltip("玩家 rig 预制体（包含玩家与相机）。留空则从 Resources 按下方路径加载。")]
    [SerializeField] private GameObject playerRigPrefab;

    [Tooltip("playerRigPrefab 为空时的 Resources 加载路径")]
    [SerializeField] private string resourcePath = "PlayerRig";

    [Tooltip("用于判断玩家是否已存在的 Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("进入副本时的出生定位")]
    [Tooltip("副本表（留空则自动从 Resources 加载 DungeonDatabase）")]
    [SerializeField] private DungeonDatabase dungeonDatabase;

    [Tooltip("是否对齐出生点朝向")]
    [SerializeField] private bool alignRotation = true;

    [Tooltip("定位前等待一个物理帧，避免与玩家自身的初始化/物理写入冲突")]
    [SerializeField] private bool waitForFixedUpdate = true;

    private void Awake()
    {
        EnsurePlayerRig();
    }

    private IEnumerator Start()
    {
        // 仅在通过传送门进入副本（存在启动上下文）时才定位。
        if (!DungeonLaunchContext.HasValue)
            yield break;

        if (dungeonDatabase == null)
            dungeonDatabase = Resources.Load<DungeonDatabase>("DungeonDatabase");

        if (dungeonDatabase == null ||
            !dungeonDatabase.TryGetById(DungeonLaunchContext.DungeonId, out DungeonDatabase.Entry entry))
            yield break;

        // 防止在非该副本场景（例如返回主城）时误定位：仅当当前场景与配置场景一致才执行。
        if (!IsCurrentScene(entry.SceneKey))
            yield break;

        if (waitForFixedUpdate)
            yield return new WaitForFixedUpdate();

        GameObject player = GameObject.FindWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning(
                $"[PlayerRigSpawner] 未找到 Tag={playerTag} 的玩家，无法定位到副本出生点。", this);
            yield break;
        }

        Vector3 pos = entry.SpawnPosition;
        Quaternion rot = alignRotation ? entry.SpawnRotation : Quaternion.identity;
        PlacePlayer(player.transform, pos, rot);
        Debug.Log($"[PlayerRigSpawner] 已将玩家放置到副本出生点（副本ID={entry.Id}，坐标={pos}）。");
    }

    private void EnsurePlayerRig()
    {
        if (!string.IsNullOrEmpty(playerTag) && GameObject.FindWithTag(playerTag) != null)
            return; // 已有保留下来的玩家，无需再生成

        GameObject prefab = playerRigPrefab != null
            ? playerRigPrefab
            : Resources.Load<GameObject>(resourcePath);

        if (prefab == null)
        {
            Debug.LogError(
                $"[PlayerRigSpawner] 未找到玩家 rig 预制体（字段为空且 Resources/{resourcePath} 不存在）。", this);
            return;
        }

        LifetimeScope scope = LifetimeScope.Find<GameLifetimeScope>();
        if (scope == null || scope.Container == null)
        {
            Debug.LogError(
                "[PlayerRigSpawner] 未找到 GameLifetimeScope 的全局容器，无法注入玩家依赖。", this);
            return;
        }

        // 先以非激活状态实例化，注入完成后再激活，避免 OnEnable 早于依赖注入
        bool prefabWasActive = prefab.activeSelf;
        prefab.SetActive(false);

        GameObject rig = Instantiate(prefab);

        prefab.SetActive(prefabWasActive);

        scope.Container.InjectGameObject(rig);
        DontDestroyOnLoad(rig);
        rig.SetActive(true);

        Debug.Log("[PlayerRigSpawner] 已生成并注入玩家 rig（跨场景保留）。");
    }

    /// <summary>
    /// 判断表里配置的场景 Key（如 "Scenes/Dungeon01"）是否就是当前激活场景（如 "Dungeon01"）。
    /// </summary>
    private static bool IsCurrentScene(string sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey))
            return true; // 未配置场景时不做场景限制

        string active = SceneManager.GetActiveScene().name;
        return sceneKey == active
            || sceneKey.EndsWith("/" + active)
            || sceneKey.EndsWith(active);
    }

    private void PlacePlayer(Transform player, Vector3 pos, Quaternion rot)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 运动学刚体不支持写速度，写了会报警告；仅对非运动学刚体清零速度。
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.position = pos;
            if (alignRotation) rb.rotation = rot;
        }

        if (alignRotation)
            player.SetPositionAndRotation(pos, rot);
        else
            player.position = pos;
    }
}
