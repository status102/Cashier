using Cashier.Commons;
using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cashier.Model;
public class TradeHistory
{
    private const string TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
    private const char PART_SPLIT = ';';
    private const char ITEM_SPLIT = ',';
    private const char COUNT_SPLIT = 'x';

    public string Time { get; init; } = DateTime.Now.ToString(TIME_FORMAT);
    /// <summary>
    /// 是否显示该项目
    /// </summary>
    public bool Visible = true;
    public bool Status { get; init; } = true;
    public string Target { get; init; } = "";
    public uint GiveGil { get; init; } = 0;
    public uint ReceiveGil { get; init; } = 0;
    public HistoryItem[] giveItemArray { get; init; } = [];
    public HistoryItem[] receiveItemArray { get; init; } = [];

    public static TradeHistory? ParseFromString(string str)
    {
        string[] strArray = str.Split(PART_SPLIT);
        if (strArray.Length != 7) {
            return null;
        }

        TradeHistory tradeHistory = new()
        {
            Time = strArray[0],
            Status = bool.Parse(strArray[1]),
            Target = strArray[2],
            GiveGil = uint.Parse(strArray[3]),
            ReceiveGil = uint.Parse(strArray[4]),
            giveItemArray = strArray[5].Split(ITEM_SPLIT).Select(i => i.Split(COUNT_SPLIT)).Where(i => i.Length == 2).Select(i => new HistoryItem(i[0], int.Parse(i[1]))).ToArray(),
            receiveItemArray = strArray[6].Split(ITEM_SPLIT).Select(i => i.Split(COUNT_SPLIT)).Where(i => i.Length == 2).Select(i => new HistoryItem(i[0], int.Parse(i[1]))).ToArray()
        };
        return tradeHistory;
    }

    public override string ToString()
    {
        List<string> write = new() {
                Time, Convert.ToString(Status), Target,
                Convert.ToString(GiveGil), Convert.ToString(ReceiveGil),
                string.Join<HistoryItem>(ITEM_SPLIT, giveItemArray),
                string.Join<HistoryItem>(ITEM_SPLIT, receiveItemArray)
            };
        return string.Join(PART_SPLIT, write);
    }

}

public class HistoryItem
{
    private const char COUNT_SPLIT = 'x';
    public string Name { get; init; }
    public int Count { get; init; }
    private bool Quality { get; init; } = false;
    public IDalamudTextureWrap? Icon { get; init; }

    public HistoryItem(string name, int count)
    {
        Name = name;
        Count = count;
        Quality = name.EndsWith("HQ");
        var itemByName = Svc.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.FirstOrDefault(r => r.Name == name.Replace("HQ", String.Empty));
        if (itemByName != null) {
            Icon = PluginUI.GetIcon(itemByName.Icon, Quality);
        }
    }
    public HistoryItem(TradeItem tradeItem)
    {
        Name = tradeItem.Name;
        Count = tradeItem.Count;
        Quality = tradeItem.Quality;
        Icon = PluginUI.GetIcon(tradeItem.IconId, Quality);
    }
    public HistoryItem(ushort iconId, string name, int count, bool quality)
    {
        Name = name;
        Count = count;
        Quality = quality;
        Icon = PluginUI.GetIcon(iconId, quality);
    }
    public override string ToString() => $"{Name.Replace(SeIconChar.HighQuality.ToIconString(), "HQ")}{COUNT_SPLIT}{Count}";
    public string ToShowString() => $"{Name} {COUNT_SPLIT} {Count}";
}
