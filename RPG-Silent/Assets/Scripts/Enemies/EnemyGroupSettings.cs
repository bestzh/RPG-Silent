using System;
using UnityEngine;

[Serializable]
public sealed class EnemyGroupSettings
{
    [Min(1)] public int MaxAttackers = 2;
    [Min(1)] public int MaxMeleeAttackers = 1;
    [Min(1)] public int MaxRangedAttackers = 1;
    [Min(0f)] public float AttackStartInterval = 0.8f;
    [Min(1)] public int AttacksPerBurst = 2;
    [Min(0f)] public float BurstPause = 1.4f;
    [Min(0f)] public float HandoffDelay = 0.25f;
    [Min(0.2f)] public float RequestLifetime = 0.6f;
    [Min(0f)] public float AllyAlertRadius = 12f;
    [Min(0.2f)] public float PositionRefreshInterval = 0.6f;
    [Min(0.2f)] public float PositionTolerance = 0.65f;
}
