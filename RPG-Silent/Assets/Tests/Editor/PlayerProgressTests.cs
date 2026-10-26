using System;
using NUnit.Framework;

namespace RPGSilent.Tests
{
    public class PlayerProgressTests
    {
        private object stats;
        [SetUp] public void SetUp() => stats = GameAssembly.New("RPGSilent.Domain.PlayerStats", 100, 100f);

        [TestCase(0, 1, 0)]
        [TestCase(99, 1, 99)]
        [TestCase(100, 2, 0)]
        [TestCase(299, 2, 199)]
        [TestCase(300, 3, 0)]
        [TestCase(850, 4, 250)]
        public void TotalExperienceMapsToExpectedLevel(int experience, int level, int inLevel)
        {
            GameAssembly.Call(stats, "AddExp", experience);
            Assert.That(GameAssembly.Property<int>(stats, "Exp"), Is.EqualTo(experience));
            Assert.That(GameAssembly.Property<int>(stats, "Level"), Is.EqualTo(level));
            Assert.That(GameAssembly.Property<int>(stats, "ExpInLevel"), Is.EqualTo(inLevel));
        }

        [Test] public void LevelUpPreservesMissingHealthAndEnergy()
        {
            GameAssembly.Call(stats, "TakeDamage", 40);
            GameAssembly.Call(stats, "TrySpendEnergy", 30f);
            GameAssembly.Call(stats, "AddExp", 100);
            Assert.That(GameAssembly.Property<int>(stats, "CurrentHealth"), Is.EqualTo(70));
            Assert.That(GameAssembly.Property<float>(stats, "CurrentEnergy"), Is.EqualTo(75f));
        }

        [Test] public void RewardNotifiesOnceWithCoherentGoldAndExperience()
        {
            int notifications = 0;
            Action listener = () =>
            {
                notifications++;
                Assert.That(GameAssembly.Property<int>(stats, "Gold"), Is.EqualTo(50));
                Assert.That(GameAssembly.Property<int>(stats, "Exp"), Is.EqualTo(125));
            };
            stats.GetType().GetEvent("OnProgressChanged").AddEventHandler(stats, listener);
            GameAssembly.Call(stats, "AddReward", 50, 125);
            GameAssembly.Call(stats, "AddReward", -1, -1);
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test] public void ExperienceCannotResurrectDeadPlayer()
        {
            GameAssembly.Call(stats, "TakeDamage", int.MaxValue);
            GameAssembly.Call(stats, "AddExp", 1000);
            Assert.That(GameAssembly.Property<bool>(stats, "IsDead"), Is.True);
            Assert.That(GameAssembly.Property<int>(stats, "CurrentHealth"), Is.Zero);
        }

        [Test] public void RewardsSaturateAndRespectLevelCap()
        {
            GameAssembly.Call(stats, "AddReward", int.MaxValue, int.MaxValue);
            GameAssembly.Call(stats, "AddReward", 10, 10);
            Assert.That(GameAssembly.Property<int>(stats, "Gold"), Is.EqualTo(int.MaxValue));
            Assert.That(GameAssembly.Property<int>(stats, "Exp"), Is.EqualTo(int.MaxValue));
            Assert.That(GameAssembly.Property<int>(stats, "Level"), Is.EqualTo(50));
            Assert.That(GameAssembly.Property<int>(stats, "ExpToNextLevel"), Is.Zero);
        }

        [Test] public void RestoreIsIdempotentAndNewGameClearsGrowth()
        {
            GameAssembly.Call(stats, "RestoreProgress", 70, 300);
            GameAssembly.Call(stats, "RestoreProgress", 70, 300);
            Assert.That(GameAssembly.Property<int>(stats, "MaxHealth"), Is.EqualTo(120));
            Assert.That(GameAssembly.Property<int>(stats, "Gold"), Is.EqualTo(70));
            GameAssembly.Call(stats, "ResetForNewGame");
            Assert.That(GameAssembly.Property<int>(stats, "Level"), Is.EqualTo(1));
            Assert.That(GameAssembly.Property<int>(stats, "MaxHealth"), Is.EqualTo(100));
            Assert.That(GameAssembly.Property<int>(stats, "Gold"), Is.Zero);
            Assert.That(GameAssembly.Property<int>(stats, "Exp"), Is.Zero);
        }

        [Test] public void InvalidRestoreLeavesExistingProgressUntouched()
        {
            GameAssembly.Call(stats, "RestoreProgress", 70, 300);
            Assert.Throws<ArgumentOutOfRangeException>(() => GameAssembly.Call(stats, "RestoreProgress", -1, 4));
            Assert.That(GameAssembly.Property<int>(stats, "Exp"), Is.EqualTo(300));
            Assert.That(GameAssembly.Property<int>(stats, "Gold"), Is.EqualTo(70));
        }

        [Test] public void InsufficientEnergyDoesNotConsumeRemainder()
        {
            Assert.That(GameAssembly.Call(stats, "TrySpendEnergy", 75f), Is.EqualTo(true));
            Assert.That(GameAssembly.Call(stats, "TrySpendEnergy", 30f), Is.EqualTo(false));
            Assert.That(GameAssembly.Property<float>(stats, "CurrentEnergy"), Is.EqualTo(25f));
            GameAssembly.Call(stats, "RecoverEnergy", 200f);
            Assert.That(GameAssembly.Property<float>(stats, "CurrentEnergy"), Is.EqualTo(100f));
        }
    }
}
