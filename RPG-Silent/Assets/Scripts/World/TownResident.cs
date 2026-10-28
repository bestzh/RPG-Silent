using UnityEngine;
using UnityEngine.AI;
using VContainer;

/// <summary>Noncombat town life: authored routes, short stops, and yielding to the player.</summary>
[RequireComponent(typeof(NavMeshAgent))]
public sealed class TownResident : MonoBehaviour
{
    public Vector3[] Waypoints = System.Array.Empty<Vector3>();
    public Animator Animator;
    public float WalkSpeed = 1.5f;
    public Vector2 RestSeconds = new(2, 5);
    public bool IsGuard;
    public Vector3 HomePosition, WorkPosition;
    [System.NonSerialized] public TownDayNight Clock;
    public string Activity { get; private set; } = "闲逛";
    public bool IsWalking => agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped && agent.velocity.sqrMagnitude > .01f;
    public int CompletedStops { get; private set; }
    NavMeshAgent agent;
    NavMeshPath path;
    PlayerSessionService session;
    PlayerController player;
    int nextPoint;
    float nextDecision, restUntil, stalledSince;
    bool travelling;
    Vector3 lastPosition;
    int schedule = -1;
    static readonly int Moving = UnityEngine.Animator.StringToHash("Moving");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>(); path = new NavMeshPath();
        if (Animator == null) Animator = GetComponentInChildren<Animator>();
        // Shared locomotion clips contain player-only footstep events.
        if (Animator != null) Animator.fireEvents = false;
    }
    void Start()
    {
        var container = GameLifetimeScope.Instance?.Container;
        if (container != null) session = container.Resolve<PlayerSessionService>();
        agent.speed = WalkSpeed; agent.stoppingDistance = .25f;
        nextPoint = Waypoints.Length > 0 ? Random.Range(0, Waypoints.Length) : 0;
        restUntil = Time.time + Random.Range(.5f, 2f); lastPosition = transform.position;
    }
    void Update()
    {
        if (Time.time < nextDecision) return;
        nextDecision = Time.time + .15f;
        if (player == null && session?.PlayerRig != null) player = session.PlayerRig.GetComponentInChildren<PlayerController>();
        if (!agent.enabled || !agent.isOnNavMesh) { if (Animator != null) Animator.SetBool(Moving, false); return; }
        int wanted = Waypoints.Length == 0 ? 0 : IsGuard ? 1 : Clock != null && Clock.IsNight ? 2 : Clock != null && Clock.Hour >= 9 && Clock.Hour < 17 ? 3 : 0;
        Activity = Waypoints.Length == 0 ? "值守" : wanted == 1 ? "巡逻" : wanted == 2 ? "休息" : wanted == 3 ? "工作" : "闲逛";
        if (schedule != wanted) { schedule = wanted; travelling = false; agent.ResetPath(); restUntil = Time.time; }
        bool yielding = player != null && (player.transform.position - transform.position).sqrMagnitude < 2.2f * 2.2f;
        agent.isStopped = yielding || !travelling;
        if (yielding)
        {
            stalledSince = 0;
            Vector3 toward = player.transform.position - transform.position; toward.y = 0;
            if (toward.sqrMagnitude > .1f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toward), 18);
        }
        else if (travelling && !agent.pathPending)
        {
            if (agent.pathStatus != NavMeshPathStatus.PathComplete || agent.remainingDistance < .45f)
                Rest();
            else if ((transform.position - lastPosition).sqrMagnitude < .0025f)
            {
                if (stalledSince <= 0) stalledSince = Time.time;
                else if (Time.time - stalledSince > 6) Rest();
            }
            else { stalledSince = 0; lastPosition = transform.position; }
        }
        else if (!travelling && Time.time >= restUntil && Waypoints.Length > 0)
        {
            var destination = schedule == 2 ? HomePosition : schedule == 3 ? WorkPosition : Waypoints[nextPoint];
            if (schedule != 2 && schedule != 3) nextPoint = (nextPoint + 1) % Waypoints.Length;
            if ((destination - transform.position).sqrMagnitude > 1 && agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete)
            { agent.SetPath(path); travelling = true; agent.isStopped = false; stalledSince = 0; lastPosition = transform.position; }
            else restUntil = Time.time + 1;
        }
        if (Animator != null) Animator.SetBool(Moving, IsWalking);
    }
    void Rest()
    {
        CompletedStops++; travelling = false; agent.ResetPath(); agent.isStopped = true;
        restUntil = Time.time + Random.Range(RestSeconds.x, Mathf.Max(RestSeconds.x, RestSeconds.y));
    }
    void OnDisable()
    {
        travelling = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; }
        if (Animator != null) Animator.SetBool(Moving, false);
    }
}
