using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.Shared.Utility;

namespace RPGSilent.Enemies.BehaviorDesigner
{
    public abstract class EnemyAction : Action
    {
        protected EnemyController Enemy;
        public override void OnAwake() { base.OnAwake(); Enemy = gameObject.GetComponent<EnemyController>(); }
        protected TaskStatus Command(EnemyActionResult result) =>
            result == EnemyActionResult.Failure ? TaskStatus.Failure : TaskStatus.Success;
    }

    public abstract class EnemyCondition : Conditional
    {
        protected EnemyController Enemy;
        public override void OnAwake() { base.OnAwake(); Enemy = gameObject.GetComponent<EnemyController>(); }
        protected TaskStatus Check(bool value) => value ? TaskStatus.Success : TaskStatus.Failure;
    }

    [Category("RPG Silent/Enemies")]
    [Description("等待 0.15 秒决策节拍；攻击、硬直、转阶段期间等待动作完成。每轮只发出一个命令。")]
    public sealed class WaitForEnemyDecision : EnemyAction
    {
        public override TaskStatus OnUpdate() => Enemy != null && Enemy.TryBeginDecision() ? TaskStatus.Success : TaskStatus.Running;
    }

    [Category("RPG Silent/Enemies")]
    [Description("敌人是否需要脱战归位。")]
    public sealed class IsReturningHome : EnemyCondition
    {
        public override TaskStatus OnUpdate() => Check(Enemy != null && Enemy.CanDecide && Enemy.ReturningHome);
    }

    [Category("RPG Silent/Enemies")]
    [Description("是否已经发现或被玩家攻击；生命和距离由感知能力持续刷新。")]
    public sealed class HasEnemyAggro : EnemyCondition
    {
        public override TaskStatus OnUpdate() => Check(Enemy != null && Enemy.CanDecide && Enemy.HasAggro);
    }

    [Category("RPG Silent/Enemies")]
    [Description("玩家是否进入远程怪撤退距离；使用不同的进入/退出距离避免来回抖动。")]
    public sealed class ShouldEnemyRetreat : EnemyCondition
    {
        public override TaskStatus OnUpdate() => Check(Enemy != null && Enemy.CanDecide && Enemy.WantsRetreat);
    }

    [Category("RPG Silent/Enemies")]
    public sealed class ReturnEnemyHome : EnemyAction
    {
        public override TaskStatus OnUpdate() => Enemy != null && Enemy.CanDecide ? Command(Enemy.DecideReturn()) : TaskStatus.Failure;
    }

    [Category("RPG Silent/Enemies")]
    public sealed class RetreatFromEnemyTarget : EnemyAction
    {
        public override TaskStatus OnUpdate() => Enemy != null && Enemy.CanDecide ? Command(Enemy.DecideRetreat()) : TaskStatus.Failure;
    }

    [Category("RPG Silent/Enemies")]
    [Description("按射程、冷却、Boss 阶段和权重选招。成功表示技能已启动；节拍节点等待整个动作结束。")]
    public sealed class TryEnemyAttack : EnemyAction
    {
        public override TaskStatus OnUpdate() => Enemy != null && Enemy.CanDecide ? Command(Enemy.DecideAttack()) : TaskStatus.Failure;
    }

    [Category("RPG Silent/Enemies")]
    public sealed class ApproachEnemyTarget : EnemyAction
    {
        public override TaskStatus OnUpdate() => Enemy != null && Enemy.CanDecide ? Command(Enemy.DecideApproach()) : TaskStatus.Failure;
    }

    [Category("RPG Silent/Enemies")]
    public sealed class IdleEnemy : EnemyAction
    {
        public override TaskStatus OnUpdate() => Enemy != null && Enemy.CanDecide ? Command(Enemy.DecideIdle()) : TaskStatus.Failure;
    }

    [Category("RPG Silent/Enemies")]
    [Description("生命到达阈值后启动 Boss 转阶段；阶段、光环和过渡由 BossPhaseController 配置。")]
    public sealed class TryBossPhaseTransition : EnemyAction
    {
        public override TaskStatus OnUpdate() => Enemy != null && Enemy.TryChangeBossPhase() ? TaskStatus.Success : TaskStatus.Failure;
    }
}
