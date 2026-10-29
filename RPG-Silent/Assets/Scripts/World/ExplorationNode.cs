using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExplorationKind { Chest, Mechanism, HiddenPassage, EliteEvent, TrainingCombo, TrainingDefense, TrainingReset, ReturnToTown, ExpeditionEvent }

/// <summary>Authored scene objectives. Rewards are world drops and reset with the instance.</summary>
public sealed class ExplorationNode : MonoBehaviour
{
    static readonly HashSet<ExplorationNode> active = new();
    public static IEnumerable<ExplorationNode> Active => active;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetActive() => active.Clear();
    public ExplorationKind Kind;
    public string DisplayName;
    public string PersistentId;
    public bool Hidden;
    public Transform MovingGate;
    public Transform ChestLid;
    public GameObject RevealRoot;
    public EnemyController ElitePrefab;
    public Transform[] EnemyPoints;
    public bool Used { get; private set; }
    public bool Discovered { get; private set; }
    public string Status { get; private set; }
    public bool Reusable => Kind >= ExplorationKind.TrainingCombo && Kind <= ExplorationKind.ReturnToTown;
    public string Symbol => Kind == ExplorationKind.Chest ? "箱" : Kind == ExplorationKind.EliteEvent ? "!" : Kind == ExplorationKind.ReturnToTown ? "城" : "机";
    public bool VisibleOnMap => (!Hidden || Discovered) && (!Used || Reusable);
    DungeonRunService run;
    TrainingGround training;
    int eventAlive;
    TMPro.TextMeshPro label;
    Camera viewer;
    void Start()
    {
        if (Kind == ExplorationKind.ExpeditionEvent) return; // The event owns its moving label.
        var presentation = Resources.Load<LootPresentation>("Loot/Presentation");
        var go = new GameObject("Exploration label"); go.transform.SetParent(transform,false);
        go.transform.localPosition = Vector3.up * 1.8f;
        label = go.AddComponent<TMPro.TextMeshPro>(); label.font = presentation != null ? presentation.Font : null;
        label.fontSize = 3; label.alignment = TMPro.TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(5,1); label.color = new Color(1,.86f,.45f);
        viewer = Camera.main;
    }
    void LateUpdate()
    {
        if (label == null || viewer == null) return;
        label.gameObject.SetActive((!Hidden || Discovered) && Vector3.Distance(viewer.transform.position,transform.position)<10);
        label.text = Used && !Reusable ? Status : DisplayName;
        label.transform.rotation = viewer.transform.rotation;
    }
    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);
    public void Initialize(DungeonRunService owner) => run = owner;
    public void InitializeTraining(TrainingGround owner) => training = owner;
    public void Discover(Vector3 playerPosition) { if ((transform.position - playerPosition).sqrMagnitude < 36f) Discovered = true; }
    public bool Activate(PlayerController player)
    {
        if (player == null || !player.CanChangeEquipment || !player.CombatAllowed || Used && !Reusable ||
            Vector3.Distance(player.transform.position, transform.position) > 2.8f) return false;
        if (Reusable) return training != null && training.Activate(Kind);
        if (run == null || run.State == DungeonRunState.Leaving || run.State == DungeonRunState.Failed) return false;
        if (Kind == ExplorationKind.ExpeditionEvent)
        {
            if (!run.TryStartExpeditionEvent(player)) return false;
            Used = Discovered = true; Status = "额外事件已开始，规则与进度见副本目标。"; run.RecordExploration(); return true;
        }
        if (Kind == ExplorationKind.EliteEvent && run.State != DungeonRunState.Running) { Status = "本次已结算，重试副本可开启事件"; return false; }
        if (Kind == ExplorationKind.EliteEvent && (ElitePrefab == null || EnemyPoints == null || EnemyPoints.Length == 0)) return false;
        Used = Discovered = true;
        if (Kind == ExplorationKind.Chest)
        {
            Vector3 towardPlayer = player.transform.position - transform.position; towardPlayer.y = 0;
            run.SpawnExplorationLoot(transform.position + towardPlayer.normalized * 1.3f);
            Status = "已打开 · 靠近拾取战利品"; if (ChestLid != null) StartCoroutine(OpenChest());
        }
        else if (Kind == ExplorationKind.EliteEvent)
        {
            eventAlive = EnemyPoints.Length; Status = "击败伏击精英，解锁事件宝箱";
            foreach (var point in EnemyPoints)
            {
                var enemy = Instantiate(ElitePrefab, point.position, point.rotation, transform.parent);
                run.RegisterOptionalEnemy(enemy); enemy.ConfigureElite(); enemy.Died += EliteDied;
            }
        }
        else
        {
            Status = Kind == ExplorationKind.HiddenPassage ? "隐藏支路已发现" : "机关门已打开";
            if (RevealRoot != null) RevealRoot.SetActive(true);
            if (MovingGate != null) StartCoroutine(OpenGate());
        }
        run.RecordExploration();
        run.RecordExplorationObjective(Kind);
        run.RecordExplorationDiscovery(this);
        return true;
    }
    void EliteDied(EnemyController enemy)
    {
        enemy.Died -= EliteDied;
        if (--eventAlive != 0) return;
        Status = "精英事件完成 · 战利品已落地";
        run?.SpawnExplorationLoot(transform.position + transform.forward * 1.3f);
    }
    IEnumerator OpenGate()
    {
        foreach (var collider in MovingGate.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var obstacle in MovingGate.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>()) obstacle.enabled = false;
        Vector3 start = MovingGate.position; float elapsed = 0;
        while (elapsed < .8f && MovingGate != null)
        { elapsed += Time.deltaTime; MovingGate.position = Vector3.Lerp(start, start + Vector3.down * 4, Mathf.SmoothStep(0,1,elapsed/.8f)); yield return null; }
        if (MovingGate != null) MovingGate.gameObject.SetActive(false);
    }
    IEnumerator OpenChest()
    {
        Quaternion start = ChestLid.localRotation; float elapsed = 0;
        while (elapsed < .5f && ChestLid != null)
        { elapsed += Time.deltaTime; ChestLid.localRotation = start * Quaternion.Euler(Mathf.Lerp(0,-100,elapsed/.5f),0,0); yield return null; }
    }
}
