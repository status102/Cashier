﻿using Cashier.Commons;
using Lumina.Excel.GeneratedSheets;
using System.Linq;

namespace Cashier.Model
{
    public class TradeItem
    {
        public uint ItemId { get; private init; } = 0;
        public int Count { get; set; } = 0;
        public bool Quality { get; init; } = false;
        public ushort IconId { get; init; } = 0;
        public string Name { get; init; } = string.Empty;
        public uint StackSize { get; init; } = 1;

        public float PresetPrice = 0;
        public Preset? ItemPreset { get; init; }

        public TradeItem() { }
        public TradeItem(uint id, int count = 1, bool quality = false)
        {
            ItemId = id;
            Count = count;
            Quality = quality;
            var item = Svc.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(r => r.RowId == id);
            if (item == null) {
                Name = "???";
                Svc.Log.Warning($"获取物品信息错误: id={id}");
            } else {
                IconId = item.Icon;
                Name = item.Name;
                StackSize = item.StackSize;
                ItemPreset = Cashier.Instance?.Config.PresetList.FirstOrDefault(i => i.Name == Name && i.Quality == Quality);
            }
        }
    }
}
