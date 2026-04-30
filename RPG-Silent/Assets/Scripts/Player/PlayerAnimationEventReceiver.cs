using UnityEngine;

public class PlayerAnimationEventReceiver : MonoBehaviour
{
    private AttackExecutor attackExecutor;

    private void Awake()
    {
        attackExecutor = GetComponent<AttackExecutor>();
        if (attackExecutor == null)
        {
            attackExecutor = GetComponentInParent<AttackExecutor>();
        }
    }

    public void FootL()
    {
        // Left footstep animation event.
    }

    public void FootR()
    {
        // Right footstep animation event.
    }

    public void Land()
    {
        // Landing animation event.
    }

    public void AttackStart()
    {
        attackExecutor?.AttackStart();
    }

    public void AttackRelease()
    {
        attackExecutor?.AttackRelease();
    }

    public void AttackEnd()
    {
        attackExecutor?.AttackEnd();
    }

    public void Hit()
    {
        AttackRelease();
    }

    public void Shoot()
    {
        AttackRelease();
    }

    public void Aoe()
    {
        AttackRelease();
    }

    public void WeaponSwitch()
    {
        // Weapon switch animation event.
    }
}
