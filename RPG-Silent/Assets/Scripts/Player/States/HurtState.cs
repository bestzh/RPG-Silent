using UnityEngine;

public class HurtState : PlayerState
{
    readonly Vector3 source;
    readonly AttackImpact impact;
    readonly bool guardBroken;
    Vector3 direction;
    float elapsed;
    private float timer;

    public HurtState(PlayerController player) : this(player, player.transform.position + player.transform.forward, AttackImpact.Light) { }
    public HurtState(PlayerController player, Vector3 source, AttackImpact impact, bool guardBroken = false) : base(player)
    { this.source = source; this.impact = impact; this.guardBroken = guardBroken; }

    public override void Enter()
    {
        timer = HitReactionRules.Duration(impact, guardBroken);
        player.HitFeedback?.ProtectRecovery(timer);
        player.AnimationController?.CancelAnimation();
        direction = HitReactionRules.Away(player.transform.position, source, player.transform.forward);
        string family = HitReactionRules.Family(player.CurrentStance);
        string motion = guardBroken ? "Guard" + family + "Break" : "Hurt" + family + (impact == AttackImpact.Heavy
            ? "Heavy" : HitReactionRules.Direction(Quaternion.Inverse(player.FacingRotation) * -direction));
        player.animator.CrossFadeInFixedTime(motion, .045f, 0, 0);
    }

    public override void Update()
    {
        elapsed += Time.deltaTime;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            player.ReturnToLocomotion();
        }
    }

    public override void FixedUpdate()
    {
        float falloff = Mathf.Clamp01(1f - elapsed / HitReactionRules.PushDuration(impact));
        player.Motor.MoveImpulse(player.GameplayInputEnabled ? direction * (HitReactionRules.PushSpeed(impact) * falloff) : Vector3.zero, Time.fixedDeltaTime);
    }
    public override void Exit() => player.Motor.SetHorizontalVelocity(Vector3.zero);
}
