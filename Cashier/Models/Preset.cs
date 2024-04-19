using Cashier.Commons;
using Lumina.Excel.GeneratedSheets;
using System.Linq;

namespace Cashier.Model;
public class Preset
{
    public uint Id { get; private set; } = 0;
    public uint IconId { get; private set; } = 0;
    public string Name { get; private set; } = string.Empty;

    public bool Quality { get; private set; }

    public uint SetPrice { get; set; } = 0;
    public uint SetCount { get; set; } = 0;
    public uint StackPrice { get; set; } = 0;
    public uint StackSize { get; private set; }
    public bool Marketable { get; private set; }

    public Preset(string name, bool quality, uint setPrice = 0, uint setCount = 0, uint stackPrice = 0)
    {
        var item = Svc.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(i => i.Name.RawString == name);
        if (item != null) {
            Id = item.RowId;
            Name = name;
            IconId = item.Icon;
            StackSize = item.StackSize;
            Quality = quality;
            Marketable = item.ItemSearchCategory.Row != 0;

            SetPrice = setPrice;
            SetCount = setCount;
            StackPrice = stackPrice;

        }
    }

    public string GetPresetString()
    {
        if (StackPrice != 0 && StackSize == 1) { return $"{StackPrice:#,0}/个"; }
        if ((SetCount == 0 || SetPrice == 0) && StackPrice != 0) { return $"{StackPrice:#,0}/组"; }
        if (SetCount != 0 && SetPrice != 0 && StackPrice != 0) { return $"{SetPrice:#,0}/{SetCount}个, {StackPrice:#,0}/组"; }
        if (SetCount != 0 && SetPrice != 0 && StackPrice == 0) { return $"{SetPrice:#,0}/{SetCount}个"; }
        return $"未设定";
    }
}
