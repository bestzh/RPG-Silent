using UnityEngine;
using UnityEngine.AI;

/// <summary>导航与刚体移动的唯一写入者。</summary>
public sealed class EnemyMotor
{
    private readonly Transform transform;
    private readonly NavMeshAgent agent;
    private readonly Rigidbody body;
    private readonly bool useAgent;
    private readonly CombatStatusController statuses;
    float EffectiveSpeed => Speed * (statuses != null ? statuses.State.MovementMultiplier : 1f);
    private Vector3 direction;
    private float nextPathUpdate;
    private float lastProgressTime;
    private Vector3 lastPosition;
    private readonly NavMeshPath retreatPath = new();
    private readonly NavMeshPath tacticalPath = new();
    private static readonly float[] retreatAngles = { 0f, 35f, -35f, 70f, -70f, 105f, -105f };
    private Vector3 retreatGoal;
    private float nextRetreatProbe;
    public Vector3 Home { get; private set; }
    public void SetHome(Vector3 position) => Home = position;
    public float Speed { get; set; }
    public float TurnSpeed { get; set; }
    public bool CanNavigate => useAgent && agent != null && agent.enabled && agent.isOnNavMesh;

    public EnemyMotor(GameObject owner, bool useNavMesh, float speed, float turnSpeed)
    {
        transform = owner.transform;
        statuses = owner.GetComponent<CombatStatusController>();
        agent = owner.GetComponent<NavMeshAgent>();
        body = owner.GetComponent<Rigidbody>();
        useAgent = useNavMesh;
        Speed = speed;
        TurnSpeed = turnSpeed;
    }

    public void Initialize()
    {
        Home = lastPosition = transform.position;
        lastProgressTime = Time.time;
        if (agent != null)
        {
            agent.speed = Speed;
            agent.angularSpeed = TurnSpeed * 60f;
            agent.updateRotation = true;
            agent.autoBraking = true;
            agent.autoRepath = true;
        }
        if (body != null)
        {
            body.isKinematic = CanNavigate;
            body.useGravity = !CanNavigate;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    public bool MoveTo(Vector3 point, float stopDistance)
    {
        if (useAgent && agent != null && !CanNavigate) { direction = Vector3.zero; return false; }
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        direction = offset.normalized;
        if (CanNavigate)
        {
            agent.speed = EffectiveSpeed;
            agent.stoppingDistance = stopDistance;
            agent.isStopped = false;
            if (Time.time >= nextPathUpdate)
            {
                nextPathUpdate = Time.time + 0.2f;
                if (!agent.SetDestination(point)) return false;
            }
            if (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete) return false;
        }
        else Face(offset);
        if (Vector3.Distance(lastPosition, transform.position) >= 0.05f || offset.magnitude <= stopDistance + 0.4f)
        { lastProgressTime = Time.time; lastPosition = transform.position; }
        return Time.time - lastProgressTime < 4f;
    }

    public bool Retreat(Vector3 target, float distance)
    {
        if (CanNavigate && Time.time < nextRetreatProbe && Vector3.Distance(transform.position, retreatGoal) > 0.5f)
            return MoveTo(retreatGoal, 0.3f);
        Vector3 away = transform.position - target;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;
        Vector3 point = target + away.normalized * distance;
        if (CanNavigate)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            float step = Mathf.Clamp(distance - away.magnitude, 2f, 4f);
            bool found = false;
            foreach (float angle in retreatAngles)
            {
                Vector3 candidate = transform.position + Quaternion.Euler(0f, angle, 0f) * away.normalized * step;
                if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, filter)) continue;
                Vector3 gap = hit.position - target;
                gap.y = 0f;
                if (gap.magnitude < away.magnitude + 0.5f ||
                    !NavMesh.CalculatePath(transform.position, hit.position, filter, retreatPath) ||
                    retreatPath.status != NavMeshPathStatus.PathComplete) continue;
                point = hit.position;
                found = true;
                break;
            }
            if (!found) return false;
            retreatGoal = point;
            nextRetreatProbe = Time.time + 0.4f;
        }
        return MoveTo(point, 0.3f);
    }

    public void Stop()
    {
        direction = Vector3.zero;
        lastProgressTime = Time.time; lastPosition = transform.position;
        if (CanNavigate) { agent.ResetPath(); agent.isStopped = true; }
        if (body != null && !body.isKinematic)
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
    }

    public bool TryReachablePoint(Vector3 candidate, out Vector3 point)
    {
        point = default;
        if (!CanNavigate) return false;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(candidate, out var hit, 0.8f, filter) ||
            !NavMesh.CalculatePath(transform.position, hit.position, filter, tacticalPath) ||
            tacticalPath.status != NavMeshPathStatus.PathComplete) return false;
        point = hit.position;
        return true;
    }

    public void Face(Vector3 offset, bool snap = false)
    {
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.001f) return;
        Quaternion rotation = Quaternion.LookRotation(offset);
        transform.rotation = snap ? rotation : Quaternion.RotateTowards(transform.rotation, rotation, Time.deltaTime * TurnSpeed * 60f);
    }

    public void FixedTick(bool moving)
    {
        if (CanNavigate) return;
        Vector3 velocity = moving ? direction * EffectiveSpeed : Vector3.zero;
        if (body != null && !body.isKinematic)
            body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
        else if (moving) transform.position += velocity * Time.fixedDeltaTime;
    }

    public void Knockback(Vector3 source, float distance)
    {
        Vector3 away = transform.position - source;
        away.y = 0f;
        Vector3 destination = transform.position + away.normalized * distance;
        if (CanNavigate)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(destination, out var hit, distance, filter) &&
                !NavMesh.Raycast(transform.position, hit.position, out _, filter)) agent.Warp(hit.position);
        }
        else if (body != null && !body.isKinematic) body.MovePosition(destination);
    }
}
