using Cashier.Model;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Cashier
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; } = 0;

		/// <summary>
		/// 是否显示交易监控窗口
		/// </summary>
		public bool ShowTradeWindow = true;

        public int TradeStepping_1 = 10;
        public int TradeStepping_2 = 50;

		#region Init and Save
		[NonSerialized]
		private DalamudPluginInterface? pluginInterface;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
			this.pluginInterface = pluginInterface;
            PropertyChanged += Configuration_PropertyChanged;
		}

        private void Configuration_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        public void Save()
        {
			PresetList = PresetList.Where(i => i.Id != 0).ToList();
			this.pluginInterface!.SavePluginConfig(this);
		}
		#endregion
	}
}
