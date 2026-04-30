using System.Collections.Generic;
using UnityEngine;

public class AttackHitboxController : MonoBehaviour
{
    [SerializeField] private List<AttackHitbox> hitboxes = new List<AttackHitbox>();

    private void Awake()
    {
        RefreshHitboxes();
        DisableAll();
    }

    public void EnableHitbox(AttackProfile profile, AttackExecutor owner)
    {
        if (profile == null)
        {
            return;
        }

        RefreshHitboxes();

        bool enabledAny = false;
        foreach (AttackHitbox hitbox in hitboxes)
        {
            if (hitbox == null)
            {
                continue;
            }

            if (!MatchesProfile(hitbox, profile))
            {
                continue;
            }

            hitbox.EnableHitbox(profile, owner);
            enabledAny = true;
        }

        if (!enabledAny)
        {
            Debug.LogWarning($"AttackProfile {profile.name} 没有找到可用 Hitbox。", this);
        }
    }

    private static bool MatchesProfile(AttackHitbox hitbox, AttackProfile profile)
    {
        bool wantsName = !string.IsNullOrEmpty(profile.HitboxName);
        bool wantsGroup = !string.IsNullOrEmpty(profile.HitboxGroup);

        if (!wantsName && !wantsGroup)
        {
            return true;
        }

        return wantsName && hitbox.HitboxName == profile.HitboxName
            || wantsGroup && hitbox.HitboxGroup == profile.HitboxGroup;
    }

    public void DisableAll()
    {
        RefreshHitboxes();

        foreach (AttackHitbox hitbox in hitboxes)
        {
            hitbox?.DisableHitbox();
        }
    }

    private void RefreshHitboxes()
    {
        hitboxes.RemoveAll(hitbox => hitbox == null);

        foreach (AttackHitbox hitbox in GetComponentsInChildren<AttackHitbox>(includeInactive: true))
        {
            if (!hitboxes.Contains(hitbox))
            {
                hitboxes.Add(hitbox);
            }
        }
    }
}
