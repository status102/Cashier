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
    public class Configuration : IPluginConfiguration, INotifyPropertyChanged
    {
        public int Version { get; set; } = 0;

        /// <summary>
        /// 是否显示交易监控窗口
        /// </summary>
        public bool ShowTradeWindow = true;

        /// <summary>
        /// 交易结束时是否在聊天框通知
        /// </summary>
        public bool TradeNotify = true;

        public List<Preset> PresetList = [];

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
