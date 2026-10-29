using System;
using UnityEngine;

public sealed partial class EnemyController
{
    Vector3[] expeditionRoute;
    int expeditionWaypoint;
    float expeditionMoveSpeed;
    public bool IsExpeditionPursuit => expeditionRoute != null;
    public event Action<EnemyController> ExpeditionEscaped;
    public bool ConfigureExpeditionPursuit(Vector3[] route, float speed)
    {
        if (initialized || IsDead || IsBoss || IsCompanion || IsTrainingTarget || route == null || route.Length < 2 || speed <= 0 || float.IsNaN(speed) || float.IsInfinity(speed)) return false;
        expeditionRoute = (Vector3[])route.Clone(); expeditionMoveSpeed = speed;
        return true;
    }
    void TickExpeditionPursuit()
    {
        HasAggro = false; ReturningHome = false;
        motor.Speed = expeditionMoveSpeed;
        if (Vector3.Distance(transform.position, expeditionRoute[expeditionWaypoint]) < .65f)
        {
            if (++expeditionWaypoint == expeditionRoute.Length)
            { motor.Stop(); ExpeditionEscaped?.Invoke(this); return; }
        }
        EnterState(EnemyState.Chase);
        if (!motor.MoveTo(expeditionRoute[expeditionWaypoint], .25f)) ExpeditionEscaped?.Invoke(this);
    }
}
