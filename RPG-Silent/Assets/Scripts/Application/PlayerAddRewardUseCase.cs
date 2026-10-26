using RPGSilent.Domain;
using UnityEngine;

namespace RPGSilent.Application
{
    public class PlayerAddRewardUseCase
    {
        private readonly PlayerStats _stats;

        public PlayerAddRewardUseCase(PlayerStats stats)
        {
            _stats = stats;
        }

        public void Execute(int gold, int exp)
        {
            _stats.AddReward(gold, exp);
            Debug.Log($"[Player] 获得奖励: +{gold} 金币, +{exp} 经验。总计: {_stats.Gold} 金 / {_stats.Exp} 经验");
        }
    }
}
