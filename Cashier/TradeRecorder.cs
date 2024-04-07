using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using TradeRecorder.Common;

namespace TradeRecorder
{
	public sealed class TradeRecorder : IDalamudPlugin
	{
		public static TaskManager? TaskManager { get; private set; }
		public static string PluginName { get; } = "Cashier";
		public string Name => "Cashier";
		private const string commandName = "/ca";
		public static TradeRecorder? Instance { get; private set; }
		public PluginUI PluginUi { get; init; }
		public Configuration Config { get; init; }
		public DalamudPluginInterface PluginInterface { get; init; }
		public uint homeWorldId = 0;

		public TradeRecorder(DalamudPluginInterface pluginInterface)
		{
			Instance = this;
			PluginInterface = pluginInterface;

			Svc.Initialize(pluginInterface);
			Config = PluginInterface.GetPluginConfig() as Configuration ?? new();
			Config.Initialize(PluginInterface);

			Svc.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
			{
				HelpMessage = "/tr 打开历史记录\n /tr config|cfg 打开设置窗口"
			});

			PluginInterface.UiBuilder.Draw += DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			Svc.ClientState.Login += OnLogin;
			Svc.ClientState.Logout += OnLogout;

			PluginUi = new PluginUI(this, Config);
			homeWorldId = Svc.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;

			// Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Trade", PluginUi.Trade.TradeUpdate);
			Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", PluginUi.Trade.TradeShow);
			Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Trade", PluginUi.Trade.TradeHide);

			ECommonsMain.Init(pluginInterface, this);
			TaskManager = new();
		}

		public void Dispose()
		{
			TaskManager!.Abort();
			ECommonsMain.Dispose();

			Svc.ClientState.Login -= OnLogin;
			Svc.ClientState.Logout -= OnLogout;
			PluginInterface.UiBuilder.Draw -= DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			Svc.CommandManager.RemoveHandler(commandName);
			//Svc.AddonLifecycle.UnregisterListener([PluginUi.Trade.TradeShow, PluginUi.Trade.TradeHide]);
			PluginUi.Dispose();
		}

		private unsafe void OnCommand(string command, string args)
		{
			string arg = args.Trim().Replace("\"", string.Empty);
			if (string.IsNullOrEmpty(arg)) {
				PluginUi.History.ShowHistory();
			} else if (arg == "cfg" || arg == "config") {
				PluginUi.Setting.Show();
			} else if (arg == "tt") {
				PluginUi.Trade.TradeItem(10386, 10);
			}else if (arg.StartsWith("money")) {
				if (!int.TryParse(arg[5..].Trim(), out int result)) {
					Common.Chat.PrintLog("money parse error" + arg);
					return;
				}
				PluginUi.Trade.TradeGil(result);
			}
#if DEBUG
			else if (arg == "test") {
				Common.Chat.PrintLog("服务器id:" + homeWorldId);
			}
#endif
		}

		private void DrawUI()
		{
			PluginUi.Draw();
		}

		private void DrawConfigUI()
		{
			this.PluginUi.Setting.Show();
		}

		private void OnLogin()
		{
			homeWorldId = Svc.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;
		}

		private void OnLogout()
		{
			homeWorldId = 0;
		}
	}
}
