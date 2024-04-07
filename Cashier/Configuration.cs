using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using TradeRecorder.Model;

namespace TradeRecorder
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; } = 0;

		/// <summary>
		/// 是否显示交易监控窗口
		/// </summary>
		public bool ShowTradeWindow = true;

		/// <summary>
		/// 交易开始前，发送交易双方ID
		/// </summary>
		public ushort OpcodeOfTradeTargetInfo = 458;// 需要维护
		/// <summary>
		/// 己方物品栏
		/// </summary>
		public ushort OpcodeOfInventoryModifyHandler = 762;
		/// <summary>
		/// 对方物品栏的物品变化
		/// </summary>
		public ushort OpcodeOfItemInfo = 921;
		/// <summary>
		/// 对方物品栏的水晶、金币
		/// </summary>
		public ushort OpcodeOfCurrencyCrystalInfo = 851;
		/// <summary>
		/// 交易成功后会触发该包，但是如果没有交易任何物品，则不会触发
		/// </summary>
		public ushort OpcodeOfUpdateInventorySlot = 0x0313;
		public List<Preset> PresetList = new();

		#region Init and Save
		[NonSerialized]
		private DalamudPluginInterface? pluginInterface;

		public void Initialize(DalamudPluginInterface pluginInterface) {
			this.pluginInterface = pluginInterface;
		}
		public void Save() {
			PresetList = PresetList.Where(i => i.Id != 0).ToList();
			this.pluginInterface!.SavePluginConfig(this);
		}
		#endregion
	}
}
