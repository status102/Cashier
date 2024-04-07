using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using TradeRecorder.Common;
using TradeRecorder.Universalis;

namespace TradeRecorder.Model
{
    public class TradeItem
	{
		public uint Id { get; private init; }
		public uint Count { get; set; } = 0;
		public bool Quality { get; private init; } = false;
		public ushort? IconId { get; private init; } = 0;
		public string Name { get; private init; }
		public uint StackSize { get; private init; } = 1;
		public Price.ItemPrice ItemPrice { get; private init; }

		public int MinPrice = 0;
		public float PresetPrice = 0;
		public Preset? ItemPreset { get; private init; }
		public TradeItem() { Id = 0; }
		public TradeItem(uint id, uint count = 1, bool quality = false) {
			Id = id;
			Count = count;
			Quality = quality;
			var item = Svc.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(r => r.RowId == id);
			if (item == null) {
				Name = "Unknown";
				Svc.PluginLog.Warning($"获取物品信息错误: id={id}");
			} else {
				IconId = item.Icon;
				Name = item.Name;
				StackSize = item.StackSize;
				ItemPreset = TradeRecorder.Instance?.Config.PresetList.FirstOrDefault(i => i.Name == Name && i.Quality == Quality);
			}
			ItemPrice = Price.GetItem(id);
		}
	}
}
