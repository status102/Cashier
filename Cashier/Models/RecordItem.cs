using Cashier.Model;
using System.Collections.Generic;
using System.Linq;

namespace Cashier.Models;

public class RecordItem
{
    public uint Id { get; private init; }
    public ushort IconId { get; private init; }
    public string Name { get; private init; }
    public uint StackSize { get; private init; }
    public int NqCount { get; set; } = 0;
    public int HqCount { get; set; } = 0;
    public RecordItem(uint id, string? name, ushort iconId, uint stackSize)
    {
        Id = id;
        Name = name ?? "???";
        IconId = iconId;
        StackSize = stackSize;
    }

    public static RecordItem operator +(RecordItem a, RecordItem b)
    {
        a.NqCount += b.NqCount;
        a.HqCount += b.HqCount;
        return a;
    }
}

public static class RecordItemExtensions
{
    public static IEnumerable<RecordItem> Convert(this IEnumerable<TradeItem> source)
    {
        return source.Where(i => i.Id != 0).GroupBy(i => i.Id * (i.Quality ? -1 : 1)).Select(i =>
        {
            var item = new RecordItem(i.First().Id, i.First().Name, i.First().IconId, i.First().StackSize);
            foreach (var item1 in i) {
                item.HqCount += item1.Quality ? item1.Count : 0;
            }
            return item;
        });
    }
}

