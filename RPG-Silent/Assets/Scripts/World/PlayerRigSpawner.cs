using RPGSilent.Domain;
using UnityEngine;
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
///   则恢复生命/能量和动作状态，按配置定位；离开副本则清上下文并回到主城默认出生点。
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

    [Header("进入副本时的出生定位")]
    [Tooltip("副本表（留空则自动从 Resources 加载 DungeonDatabase）")]
    [SerializeField] private DungeonDatabase dungeonDatabase;

    [Tooltip("是否对齐出生点朝向")]
    [SerializeField] private bool alignRotation = true;

    private void Awake()
    {
        EnsurePlayerRig();
    }

    private void Start()
    {
        if (!DungeonLaunchContext.HasValue) return;

        if (dungeonDatabase == null)
            dungeonDatabase = Resources.Load<DungeonDatabase>("DungeonDatabase");

        var entry = dungeonDatabase != null ? dungeonDatabase.GetById(DungeonLaunchContext.DungeonId) : null;
        bool enteringDungeon = entry != null && entry.MatchesScene(gameObject.scene.name);
        var session = GameLifetimeScope.Instance.Container.Resolve<PlayerSessionService>();
        var player = session.PlayerRig != null ? session.PlayerRig.GetComponentInChildren<PlayerController>(true) : null;
        if (player == null) return;

        Vector3 pos;
        Quaternion rot;
        if (enteringDungeon)
        {
            pos = entry.SpawnPosition;
            rot = entry.SpawnRotation;
        }
        else
        {
            // 返回主城时使用与新游戏相同的出生点，不沿用副本世界坐标。
            var prefab = playerRigPrefab != null ? playerRigPrefab : Resources.Load<GameObject>(resourcePath);
            var origin = prefab.GetComponentInChildren<PlayerController>(true).transform;
            pos = origin.position;
            rot = origin.rotation;
            DungeonLaunchContext.Clear();
        }

        GameLifetimeScope.Instance.Container.Resolve<PlayerStats>().RestoreVitals();
        player.ResetForScene(pos, alignRotation ? rot : player.transform.rotation);
        Debug.Log($"[PlayerRigSpawner] 场景出生已重置：{gameObject.scene.name}，{pos}。");
    }

    private void EnsurePlayerRig()
    {
        var scope = GameLifetimeScope.Instance;
        if (scope == null || scope.Container == null)
        {
            Debug.LogError(
                "[PlayerRigSpawner] 未找到 GameLifetimeScope 的全局容器，无法注入玩家依赖。", this);
            return;
        }

        var session = scope.Container.Resolve<PlayerSessionService>();
        if (session.PlayerRig != null) return;
        if (!session.IsActive)
        {
            Debug.LogError("[PlayerRigSpawner] 请通过主菜单开始玩家会话。", this);
            return;
        }

        GameObject prefab = playerRigPrefab != null
            ? playerRigPrefab
            : Resources.Load<GameObject>(resourcePath);

        if (prefab == null)
        {
            Debug.LogError(
                $"[PlayerRigSpawner] 未找到玩家 rig 预制体（字段为空且 Resources/{resourcePath} 不存在）。", this);
            return;
        }

        // 先以非激活状态实例化，注入完成后再激活，避免 OnEnable 早于依赖注入
        bool prefabWasActive = prefab.activeSelf;
        prefab.SetActive(false);

        GameObject rig = Instantiate(prefab);

        prefab.SetActive(prefabWasActive);

        scope.Container.InjectGameObject(rig);
        DontDestroyOnLoad(rig);
        session.RegisterPlayerRig(rig);
        rig.SetActive(true);
        session.OnPlayerReady();

        Debug.Log("[PlayerRigSpawner] 已生成并注入玩家 rig（跨场景保留）。");
    }

}
