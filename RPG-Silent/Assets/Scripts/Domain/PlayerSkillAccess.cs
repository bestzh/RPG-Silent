using System;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 运行时技能读取桥接：玩家生成后由 PlayerSkillController 绑定，
    /// MainUI 通过 DI 注入本对象订阅槽位状态（避免 UI FindWithTag）。
    /// </summary>
    public class PlayerSkillAccess
    {
        public IPlayerSkillReader Current { get; private set; }

        public event Action<IPlayerSkillReader> OnBound;
        public event Action OnUnbound;

        public void Bind(IPlayerSkillReader reader)
        {
            Current = reader;
            OnBound?.Invoke(reader);
        }

        public void Unbind(IPlayerSkillReader reader)
        {
            if (Current != reader) return;
            Current = null;
            OnUnbound?.Invoke();
        }
    }
}
