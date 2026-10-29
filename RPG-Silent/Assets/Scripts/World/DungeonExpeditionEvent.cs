using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Scene-local event actors; durable state and the single reward receipt live in PlayerProgressService.</summary>
public sealed class DungeonExpeditionEvent : MonoBehaviour
{
    DungeonRunService run;
    PlayerProgressService progress;
    PlayerController player;
    ExpeditionData token;
    ExpeditionPlan plan;
    DungeonVariationDefinition definition;
    readonly List<EnemyController> actors = new();
    EnemyCombatGroup combatGroup;
    ExplorationNode beacon;
    Transform signal;
    NavMeshAgent escort;
    TextMeshPro label;
    Camera viewer;
    LineRenderer boundary;
    float startedAt, heldTime, lastProgress, lastMovementAt;
    Vector3 lastPosition;
    int waypoint;
    bool active, secondWave, stopped;
    ExpeditionEventStatus? pending;
    string outcome = "未开始 · 靠近金色信标交互";
    public bool NeedsSave => pending.HasValue;
    public bool Terminal { get; private set; }
    public bool Settled => Terminal && !NeedsSave;
    public ExpeditionEventKind Kind => plan.Event;
    public float TimeRemaining => Mathf.Max(0, Limit - (active ? Time.time - startedAt : 0));
    public float HeldTime => heldTime;
    public float Limit => plan.Event == ExpeditionEventKind.Escort ? 120 : plan.Event == ExpeditionEventKind.Hold ? 90 : 60;
    public string SaveError { get; private set; }
    public string Summary => $"远征种子 {token.seed} · 第 {token.attempt} 次 · {plan.EventLabel}\n" +
        (active ? $"{Mathf.CeilToInt(TimeRemaining)}秒 · " + (Kind == ExpeditionEventKind.Hold ? $"驻守 {heldTime:0.0}/25秒 · " : "") : "") + outcome +
        (!Terminal ? $" · 目标距离 {Vector3.Distance(player.transform.position, signal.position):0}米" : "") +
        (!string.IsNullOrEmpty(SaveError) ? "\n" + (NeedsSave ? "事件尚未保存：" : "无法开始事件，请再次交互：") + SaveError : "") +
        (token.rewardClaimed ? " · 本组合奖励已领取" : "");
    public IReadOnlyList<EnemyController> Actors => actors;
    public Transform Signal => signal;

    public void Initialize(DungeonRunService owner, PlayerProgressService saves, PlayerController actor, ExpeditionData receipt, ExpeditionPlan rules, DungeonVariationDefinition layout)
    {
        run = owner; progress = saves; player = actor; token = receipt.Copy(); plan = rules; definition = layout;
        transform.position = layout.Route[0];
        beacon = gameObject.AddComponent<ExplorationNode>(); beacon.Kind = ExplorationKind.ExpeditionEvent;
        beacon.PersistentId = "expedition-" + token.id; beacon.DisplayName = "远征信标 · " + plan.EventLabel; beacon.Initialize(owner);
        signal = new GameObject("Moving signal").transform; signal.SetParent(transform, false);
        var stone = GameObject.CreatePrimitive(PrimitiveType.Cube); stone.name = "Signal crystal"; stone.transform.SetParent(signal, false);
        stone.transform.localPosition = Vector3.up * 1.25f; stone.transform.localRotation = Quaternion.Euler(0, 45, 45); stone.transform.localScale = new Vector3(.45f, .8f, .45f);
        stone.GetComponent<Collider>().enabled = false;
        stone.GetComponent<Renderer>().sharedMaterial = layout.MarkerMaterial;
        var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", new Color(1, .72f, .15f)); block.SetColor("_Color", new Color(1, .72f, .15f)); stone.GetComponent<Renderer>().SetPropertyBlock(block);
        boundary = Line(signal, "Event boundary", new Color(1, .75f, .2f), true);
        boundary.positionCount = 65;
        for (int i = 0; i <= 64; i++) boundary.SetPosition(i, new Vector3(Mathf.Cos(i * Mathf.PI / 32) * (Kind == ExpeditionEventKind.Hold ? 3 : .9f), .12f, Mathf.Sin(i * Mathf.PI / 32) * (Kind == ExpeditionEventKind.Hold ? 3 : .9f)));
        var route = Line(transform, "Safe route", new Color(.3f, .9f, 1, .8f), false);
        route.positionCount = definition.Route.Length;
        route.SetPositions(definition.Route.Select(v => v + Vector3.up * .12f).ToArray());
        route.gameObject.SetActive(Kind != ExpeditionEventKind.Hold);
        var text = new GameObject("Event status"); text.transform.SetParent(signal, false); text.transform.localPosition = Vector3.up * 2.5f;
        label = text.AddComponent<TextMeshPro>(); label.font = Resources.Load<LootPresentation>("Loot/Presentation").Font;
        label.fontSize = .65f; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(6, 1.8f); label.raycastTarget = false;
        label.color = new Color(1, .85f, .5f);
        viewer = Camera.main;
    }
    LineRenderer Line(Transform parent, string name, Color color, bool local)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = definition.MarkerMaterial; line.useWorldSpace = !local;
        line.startWidth = line.endWidth = .08f; line.startColor = line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false; return line;
    }
    public bool TryStart()
    {
        if (stopped || active || Terminal || run.State != DungeonRunState.Running || player.IsDead) return false;
        if (!progress.TrySetExpeditionState(token, ExpeditionEventStatus.Active, out var message)) { SaveError = message; run.ExpeditionChanged(); return false; }
        SaveError = null;
        active = true; startedAt = Time.time; combatGroup = new EnemyCombatGroup(new EnemyGroupSettings());
        outcome = plan.EventRules;
        if (Kind == ExpeditionEventKind.Pursuit)
        {
            var route = definition.Route.Skip(1).Concat(definition.Route).Concat(definition.Route).ToArray();
            var target = Spawn(definition.Melee, definition.Route[0], true, route);
            signal.SetParent(target.transform, false);
            var crystal = signal.Find("Signal crystal"); crystal.localPosition = Vector3.up * 2.4f; crystal.localScale *= .5f;
            label.transform.localPosition = Vector3.up * 3.2f;
            target.ExpeditionEscaped += Escaped; target.Died += PursuitDefeated;
            outcome = "追上并击败金色精英信使；绕行三圈后逃脱";
        }
        else
        {
            SpawnWave();
            if (Kind == ExpeditionEventKind.Escort)
            {
                var go = new GameObject("Escort navigation"); go.SetActive(false); go.transform.position = definition.Route[0];
                escort = go.AddComponent<NavMeshAgent>(); escort.agentTypeID = definition.AgentType; escort.radius = .25f; escort.height = 1.2f;
                escort.speed = 1.8f; escort.acceleration = 6; escort.stoppingDistance = .2f; escort.autoBraking = true;
                go.transform.SetParent(transform, true); go.SetActive(true); signal.SetParent(go.transform, false); waypoint = 1;
                lastPosition = escort.transform.position; lastMovementAt = Time.time;
            }
        }
        run.ExpeditionChanged(); return true;
    }
    EnemyController Spawn(EnemyController prefab, Vector3 point, bool pursuit = false, Vector3[] route = null)
    {
        var enemy = Instantiate(prefab, point, Quaternion.identity, transform);
        if (pursuit) { enemy.ConfigureElite(); if (!enemy.ConfigureExpeditionPursuit(route, 1.8f)) throw new InvalidOperationException("追击目标不可配置。"); }
        else enemy.BindCombatGroup(combatGroup);
        actors.Add(enemy); run.RegisterOptionalEnemy(enemy); return enemy;
    }
    void SpawnWave()
    {
        Spawn(definition.Melee, definition.AmbushPoints[0]); Spawn(definition.Ranged, definition.AmbushPoints[1]);
    }
    void Escaped(EnemyController enemy) => End(ExpeditionEventStatus.Failed, "信使已逃脱 · 无额外奖励");
    void PursuitDefeated(EnemyController enemy) => End(ExpeditionEventStatus.Completed, "信使已击败");
    void Update()
    {
        if (!active || stopped || Terminal) return;
        if (run.State != DungeonRunState.Running || player.IsDead) { End(ExpeditionEventStatus.Failed, "角色倒下，事件失败"); return; }
        if (TimeRemaining <= 0) { End(ExpeditionEventStatus.Failed, "事件超时 · 无额外奖励"); return; }
        bool clear = actors.All(e => e == null || e.IsDead);
        if (Kind == ExpeditionEventKind.Hold)
        {
            if (!secondWave && Time.time - startedAt >= 12) { secondWave = true; SpawnWave(); clear = false; }
            bool inside = Vector3.Distance(player.transform.position, definition.Route[0]) <= 3;
            bool contested = actors.Any(e => e != null && !e.IsDead && Vector3.Distance(e.transform.position, definition.Route[0]) <= 3);
            if (inside && !contested) heldTime += Time.deltaTime;
            outcome = !inside ? "返回3米金圈继续驻守" : contested ? "据点有敌人 · 清除后继续计时" : "据点安全 · 正在累计驻守";
            if (heldTime >= 25 && secondWave && clear) End(ExpeditionEventStatus.Completed, "据点守卫成功");
        }
        else if (Kind == ExpeditionEventKind.Escort)
        {
            if (escort == null || !escort.isOnNavMesh) { End(ExpeditionEventStatus.Failed, "护送路线失效，请重试"); return; }
            bool close = Vector3.Distance(player.transform.position, escort.transform.position) <= 6;
            bool contested = actors.Any(e => e != null && !e.IsDead && Vector3.Distance(e.transform.position, escort.transform.position) <= 2.5f);
            if (!close || contested)
            {
                escort.isStopped = true; lastMovementAt = Time.time;
                outcome = !close ? "保持在信标6米内继续护送" : "敌人阻挡信标 · 清除后继续";
            }
            else if (waypoint < definition.Route.Length)
            {
                escort.isStopped = false; escort.SetDestination(definition.Route[waypoint]); outcome = $"护送至路标 {waypoint + 1}/{definition.Route.Length}";
                if (!escort.pathPending && escort.pathStatus != NavMeshPathStatus.PathComplete) { End(ExpeditionEventStatus.Failed, "护送路径失效，请重试"); return; }
                if (Vector3.Distance(escort.transform.position, definition.Route[waypoint]) <= .6f) { waypoint++; lastMovementAt = Time.time; }
                if (Vector3.Distance(lastPosition, escort.transform.position) >= .1f) { lastPosition = escort.transform.position; lastMovementAt = Time.time; }
                if (Time.time - lastMovementAt > 6) { End(ExpeditionEventStatus.Failed, "护送信标受阻，请重试"); return; }
            }
            else { escort.isStopped = true; outcome = "已抵终点 · 清除剩余事件敌人"; if (clear) End(ExpeditionEventStatus.Completed, "护送成功"); }
        }
        if (Time.time - lastProgress > .2f) { lastProgress = Time.time; run.ExpeditionChanged(); }
    }
    void LateUpdate()
    {
        if (label == null || viewer == null) return;
        label.transform.rotation = viewer.transform.rotation;
        label.gameObject.SetActive(Vector3.Distance(viewer.transform.position, signal.position) < 45);
        label.text = plan.EventLabel + (active ? $" · {Mathf.CeilToInt(TimeRemaining)}秒" : "") + "\n" + outcome;
    }
    void End(ExpeditionEventStatus state, string text)
    {
        if (Terminal || stopped) return;
        Terminal = true; active = false; outcome = text; pending = state;
        StopActors(); RetrySave();
    }
    public bool RetrySave()
    {
        if (!pending.HasValue) return true;
        if (!progress.TrySetExpeditionState(token, pending.Value, out var message)) { SaveError = message; run.ExpeditionChanged(); return false; }
        token = progress.Expedition; pending = null; SaveError = null; run.ExpeditionChanged(); return true;
    }
    public void Abandon() => End(ExpeditionEventStatus.Abandoned, "已放弃额外事件 · 普通掉落保留");
    public void OnRunFinished()
    {
        if (Terminal) StopActors();
        else if (active) End(ExpeditionEventStatus.Failed, "角色倒下，事件失败");
        else Abandon();
    }
    void StopActors()
    {
        if (escort != null && escort.isOnNavMesh) { escort.isStopped = true; escort.enabled = false; }
        combatGroup?.Stop();
        foreach (var enemy in actors)
            if (enemy != null) { enemy.Died -= PursuitDefeated; enemy.ExpeditionEscaped -= Escaped; if (!enemy.IsDead) run.RemoveExpeditionEnemy(enemy); }
    }
    public void Stop() { if (stopped) return; stopped = true; active = false; StopActors(); }
    void OnDestroy() => Stop();
}
