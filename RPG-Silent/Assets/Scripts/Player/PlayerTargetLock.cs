using System.Collections.Generic;
using UnityEngine;

/// <summary>Local, visible hostile selection. No scene-wide search in the frame loop.</summary>
[DisallowMultipleComponent]
public sealed class PlayerTargetLock : MonoBehaviour
{
    [SerializeField, Min(1)] float acquisitionRange = 24f;
    [SerializeField, Min(1)] float releaseRange = 30f;
    [SerializeField, Min(0)] float occlusionGrace = .45f;
    readonly RaycastHit[] sightHits = new RaycastHit[64];
    readonly List<EnemyController> candidates = new(32);
    PlayerController player;
    Camera viewCamera;
    Collider targetCollider;
    float hiddenSince = -1f;
    float nextVisibilityCheck;
    public EnemyController Target { get; private set; }
    public bool IsLocked => Target != null;
    public bool HasLineOfSight { get; private set; }
    public Vector3 AimPoint => Target == null ? transform.position : Point(Target, targetCollider);

    void Awake() => player = GetComponent<PlayerController>();
    bool CanSelect => player != null && player.isActiveAndEnabled && player.GameplayInputEnabled
        && player.CanReceiveCombatEffects && Cursor.lockState == CursorLockMode.Locked;
    Camera ViewCamera => viewCamera != null ? viewCamera : viewCamera = Camera.main;
    public void Toggle()
    {
        if (IsLocked) { Clear(); return; }
        if (!CanSelect) return;
        Gather();
        EnemyController best = null;
        float bestScore = float.PositiveInfinity;
        foreach (var candidate in candidates)
        {
            Vector3 point = Point(candidate, candidate.GetComponent<Collider>());
            Vector3 viewport = ViewCamera.WorldToViewportPoint(point);
            float score = new Vector2(viewport.x - .5f, viewport.y - .5f).sqrMagnitude * 100f
                + Vector3.Distance(transform.position, point) * .08f;
            if (score < bestScore) { best = candidate; bestScore = score; }
        }
        SetTarget(best);
    }
    public void Cycle(int direction)
    {
        if (!CanSelect || !IsLocked || direction == 0) return;
        Gather();
        if (candidates.Count == 0) return;
        // Screen-left/right order remains stable when several colliders belong to one enemy.
        candidates.Sort((a, b) =>
        {
            int order = ViewCamera.WorldToViewportPoint(a.transform.position).x.CompareTo(ViewCamera.WorldToViewportPoint(b.transform.position).x);
            return order != 0 ? order : a.GetInstanceID().CompareTo(b.GetInstanceID());
        });
        int index = candidates.IndexOf(Target);
        int next = index < 0 ? (direction > 0 ? 0 : candidates.Count - 1)
            : (index + (direction > 0 ? 1 : -1) + candidates.Count) % candidates.Count;
        SetTarget(candidates[next]);
    }
    void Gather()
    {
        candidates.Clear();
        if (ViewCamera == null) return;
        // Forest scenery can fill a physics overlap buffer before any enemy is returned.
        // The lifecycle registry contains only actors and needs no scene search or allocation.
        foreach (var enemy in EnemyController.ActiveEnemies)
        {
            if (!Valid(enemy) || (enemy.transform.position - transform.position).sqrMagnitude > acquisitionRange * acquisitionRange) continue;
            Vector3 point = Point(enemy, enemy.GetComponent<Collider>());
            Vector3 screen = ViewCamera.WorldToViewportPoint(point);
            if (screen.z <= 0 || screen.x < .04f || screen.x > .96f || screen.y < .04f || screen.y > .96f) continue;
            if (!Visible(enemy, point)) continue;
            candidates.Add(enemy);
        }
    }
    static bool Valid(EnemyController enemy) => enemy != null && enemy.isActiveAndEnabled && !enemy.IsDead && !enemy.IsCompanion && !enemy.CombatLocked;
    static Vector3 Point(EnemyController enemy, Collider collider) => collider != null && collider.enabled
        ? collider.bounds.center : enemy.transform.position + Vector3.up;
    bool Visible(EnemyController enemy, Vector3 point)
    {
        if (ViewCamera == null) return false;
        return ClearRay(transform.position + Vector3.up * 1.2f, point, enemy)
            && ClearRay(ViewCamera.transform.position, point, enemy);
    }
    bool ClearRay(Vector3 origin, Vector3 point, EnemyController enemy)
    {
        Vector3 delta = point - origin;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, sightHits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform hit = sightHits[i].transform;
            if (hit.IsChildOf(transform) || hit.IsChildOf(enemy.transform)) continue;
            return false;
        }
        return true;
    }
    void SetTarget(EnemyController enemy)
    {
        Clear();
        if (!Valid(enemy)) return;
        Target = enemy;
        targetCollider = enemy.GetComponent<Collider>();
        Target.Died += OnTargetDied;
        HasLineOfSight = true;
    }
    void OnTargetDied(EnemyController enemy) => Clear();
    public void Clear()
    {
        if (Target != null) Target.Died -= OnTargetDied;
        Target = null; targetCollider = null; hiddenSince = -1f;
        nextVisibilityCheck = 0; HasLineOfSight = false;
    }
    public void Tick()
    {
        if (!IsLocked) return;
        if (!CanSelect || !Valid(Target) || (Target.transform.position - transform.position).sqrMagnitude > releaseRange * releaseRange)
        { Clear(); return; }
        if (Time.time < nextVisibilityCheck) return;
        nextVisibilityCheck = Time.time + .06f;
        HasLineOfSight = Visible(Target, AimPoint);
        if (HasLineOfSight) hiddenSince = -1f;
        else if (hiddenSince < 0) hiddenSince = Time.time;
        else if (Time.time - hiddenSince >= occlusionGrace) Clear();
    }
    void OnDisable() => Clear();
}
