using UnityEngine;

/// <summary>The same enemy snapshot places the warning and the eventual hit.</summary>
public static class EnemyAttackGeometry
{
    public static bool IsCircle(AttackDeliveryType delivery) => delivery == AttackDeliveryType.SelfAoe || delivery == AttackDeliveryType.AoeAtPoint ||
        delivery == AttackDeliveryType.InstantAoe || delivery == AttackDeliveryType.PersistentAoe;
    public static bool IsLine(AttackDeliveryType delivery) => delivery == AttackDeliveryType.Projectile || delivery == AttackDeliveryType.Raycast;
    public static Vector3 Center(AttackProfile profile, Vector3 origin, Quaternion rotation, Vector3? area)
    {
        if (profile.DeliveryType == AttackDeliveryType.MeleeArc || profile.DeliveryType == AttackDeliveryType.MeleeHitbox)
            return origin + rotation * Vector3.forward * profile.Range;
        if (profile.DeliveryType == AttackDeliveryType.SelfAoe) return origin + profile.AoeOffset;
        return area ?? origin + rotation * profile.AoeOffset;
    }
    public static float ProjectileRadius(AttackProfile profile)
    {
        var sphere = profile.ProjectilePrefab != null ? profile.ProjectilePrefab.GetComponent<SphereCollider>() : null;
        return sphere != null ? Mathf.Max(.03f, sphere.radius * Mathf.Abs(sphere.transform.lossyScale.x)) : .12f;
    }
}
