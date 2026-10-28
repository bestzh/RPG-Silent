using System;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    [Serializable]
    public sealed class ConsumableLoadoutData
    {
        public const int SlotCount = 3;
        public const int SharedCooldownSeconds = 8;
        public string[] itemIds = { "potion-health", "potion-energy", "potion-guard" };
        public long readyUtcTicks;
        public ConsumableLoadoutData Copy() => new() { itemIds = (string[])itemIds.Clone(), readyUtcTicks = readyUtcTicks };
        public static double Remaining(long ready, long now) => Math.Max(0, (ready - (double)now) / TimeSpan.TicksPerSecond);
        public static void Validate(ConsumableLoadoutData data)
        {
            if (data == null || data.itemIds == null || data.itemIds.Length != SlotCount ||
                data.itemIds.Any(x => x == null || x.Length > 128) || data.itemIds.Where(x => x.Length > 0).Distinct().Count() != data.itemIds.Count(x => x.Length > 0) ||
                data.readyUtcTicks < 0 || data.readyUtcTicks > DateTime.MaxValue.Ticks)
                throw new InvalidDataException("药品快捷栏或冷却记录无效。");
        }
    }
}
