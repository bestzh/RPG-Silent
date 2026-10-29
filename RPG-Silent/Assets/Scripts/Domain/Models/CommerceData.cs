using System;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    [Serializable]
    public sealed class BuybackRecordData
    {
        public string id;
        public ItemStackData item;
        public int price;
        public long soldUtcTicks;
        public bool Expired(long now) => now >= soldUtcTicks && now - soldUtcTicks >= CommerceData.RetentionTicks;
        public BuybackRecordData Copy() => new() { id = id, item = item.Copy(), price = price, soldUtcTicks = soldUtcTicks };
    }
    [Serializable]
    public sealed class CommerceData
    {
        public const int BuybackCapacity = 24;
        public static readonly long RetentionTicks = TimeSpan.FromHours(48).Ticks;
        public long revision;
        public BuybackRecordData[] buyback = Array.Empty<BuybackRecordData>();
        public CommerceData Copy() => new() { revision = revision, buyback = buyback.Select(x => x.Copy()).ToArray() };
        public bool RecordSale(ItemStackData[] sold, int[] prices, long now)
        {
            if (revision == long.MaxValue || sold == null || prices == null || sold.Length == 0 || sold.Length != prices.Length || sold.Length > BuybackCapacity ||
                now < 1 || now > DateTime.MaxValue.Ticks || prices.Any(x => x < 0) || sold.Any(x => !InventoryData.ValidItem(x) || x.legacyReserve)) return false;
            var combined = buyback.Where(x => !x.Expired(now)).Select(x => x.Copy()).Concat(sold.Select((x, i) => new BuybackRecordData {
                id = Guid.NewGuid().ToString("N"), item = x.Copy(), price = prices[i], soldUtcTicks = now
            })).ToArray();
            buyback = combined.Skip(Math.Max(0, combined.Length - BuybackCapacity)).ToArray(); revision++; return true;
        }
        public static void Validate(CommerceData data)
        {
            if (data == null || data.buyback == null || data.buyback.Length > BuybackCapacity || data.revision < 0 ||
                data.buyback.Any(x => x == null || !Guid.TryParseExact(x.id, "N", out _) || !InventoryData.ValidItem(x.item) || x.item.legacyReserve ||
                    x.price < 0 || x.soldUtcTicks < 1 || x.soldUtcTicks > DateTime.MaxValue.Ticks) || data.buyback.Select(x => x.id).Distinct().Count() != data.buyback.Length)
                throw new InvalidDataException("交易或回购记录无效，原存档已保留。");
        }
    }
}
