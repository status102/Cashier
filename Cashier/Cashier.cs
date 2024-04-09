using Cashier.Commons;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using ECommons;
using ECommons.Automation;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Runtime.InteropServices;

namespace Cashier
{
	public unsafe sealed class Cashier : IDalamudPlugin
	{
		public static TaskManager? TaskManager { get; private set; }
		public static string PluginName { get; } = "Cashier";
		public string Name => "Cashier";
		private const string commandName = "/ca";
		public static Cashier? Instance { get; private set; }
		public PluginUI PluginUi { get; init; }
		public Configuration Config { get; init; }
		public DalamudPluginInterface PluginInterface { get; init; }
		public uint homeWorldId = 0;

		public Cashier(DalamudPluginInterface pluginInterface)
		{
			Instance = this;
			PluginInterface = pluginInterface;

			Svc.Initialize(pluginInterface);
			Config = PluginInterface.GetPluginConfig() as Configuration ?? new();
			Config.Initialize(PluginInterface);

			Svc.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
			{
				HelpMessage = "/ca 打开历史记录\n /ca config|cfg 打开设置窗口"
			});

			PluginInterface.UiBuilder.Draw += DrawUI;
			PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			Svc.ClientState.Login += OnLogin;
			Svc.ClientState.Logout += OnLogout;

			PluginUi = new PluginUI(this, Config);
			homeWorldId = Svc.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;

			//Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Trade", PluginUi.Trade.TradeUpdate);
			Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", PluginUi.Trade.TradeShow);
			Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Trade", PluginUi.Trade.TradeHide);
			Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "Trade", (events, args) =>
			{
				//Svc.PluginLog.Debug("PostUpdate");
			});
			Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Trade", PluginUi.Trade.AddonTradeReceiveEvent);
			Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Trade", (events, args) =>
			{
				Svc.PluginLog.Debug("PostRefresh");
			});



			ECommonsMain.Init(pluginInterface, this);
			TaskManager = new();

			if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon)) {
				Svc.ChatGui.Print("没找到注入");
				// 无用
			} else {
				var a = addon->GetNodeById(8)->GetAsAtkComponentNode()->Component->UldManager.NodeList;
				var targetNode = (addon->GetNodeById(8)->GetAsAtkComponentNode()->Component->UldManager.NodeList[0])->GetAsAtkCollisionNode();

				Svc.AddonEventManager.AddEvent((nint)addon, (nint)targetNode, AddonEventType.InputReceived, Test);
			}
			//_resetSlotHook?.Enable();
			_setSlotItemIdHook?.Enable();
			_tradeTargetHook?.Enable();
			_tradeOtherMoney?.Enable();
			//_tradeCountHook?.Enable();
		}

		private void Test(AddonEventType atkEventType, nint atkUnitBase, nint atkResNode)
		{
			Svc.ChatGui.Print("test");
		}

		public void Dispose()
		{
			_tradeCountHook?.Dispose();
			_tradeOtherMoney?.Dispose();
			_tradeTargetHook?.Dispose();
			_resetSlotHook?.Dispose();
			_setSlotItemIdHook?.Dispose();
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
			} else if (arg == "t") {
				var id = TargetSystem.Instance()->Target;
				Commons.Chat.PrintLog($"ID:{id->ObjectID:X}||{(nint)id:X}");
				var a = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Trade");
				var imageNode = a->GetNodeById(4)->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode();

				Commons.Chat.PrintLog($"Trade:{imageNode->AtkResNode.ScaleY}");

			} else if (arg == "tt") {
			} else if (arg.StartsWith("money")) {
				if (!int.TryParse(arg[5..].Trim(), out int result)) {
					Commons.Chat.PrintLog("money parse error" + arg);
					return;
				}
				PluginUi.Trade.TradeGil(result);
			} else if (arg == "cancel") {
				PluginUi.Trade.TradeCancel();
			} else if (arg.StartsWith("trade name")) {
				PluginUi.Trade.RequestTrade(arg[10..].Trim());
			} else if (arg.StartsWith("confirm")) {
				PluginUi.Trade.TradeConfirm();
			}
#if DEBUG
			else if (arg == "test") {
				Commons.Chat.PrintLog("服务器id:" + homeWorldId);
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

		private delegate nint ResetSlot(nint a);

		[Signature("40 53 48 83 EC 20 83 79 70 00 48 8B D9 74 36", DetourName = nameof(DetourResetSlot))]
		private Hook<ResetSlot>? _resetSlotHook;

		private nint DetourResetSlot(nint a)
		{
			PluginLog.Information($"一个格子被清空: {a:X}");

			try {
				// your plugin logic goes here.
			} catch (Exception ex) {
				PluginLog.Error("An error occured when handling a macro save event." + ex.Message);
			}

			return this._resetSlotHook!.Original(a);
		}

		private delegate nint SetSlotItemId(nint a, uint itemId, nint a3, int a4, nint a5);
		[Signature("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 48 89 7C 24 20 41 56 48 83 EC 20 8B F2 41 8B E9 48 8B D9 45", DetourName = nameof(DetourSetSlotItemId))]
		private Hook<SetSlotItemId>? _setSlotItemIdHook;

		private nint DetourSetSlotItemId(nint a, uint itemId, nint a3, int a4, nint a5)
		{
			// 未能读取到物品数量，暂时搁置
			PluginLog.Information($"一个格子设置: {a:X}, itemId:{itemId}, a3:{a3}, a4:{a4}, a5:{a5}");

			try {
				// your plugin logic goes here.
			} catch (Exception ex) {
				PluginLog.Error("An error occured when handling a macro save event." + ex.Message);
			}

			return this._setSlotItemIdHook!.Original(a, itemId, a3, a4, a5);
		}

		[Signature("4C 8B DC 53 55 48 81 EC 68 01 00 00 48 8B 05 0D 56 A6 01 48 33 C4 48 89 84 24 40 01 00 00", DetourName = nameof(DetourTradeTarget))]
		private Hook<TradeTarget>? _tradeTargetHook;
		private delegate nint TradeTarget(nint a1, nint a2, nint a3);
		private nint DetourTradeTarget(nint a1, nint objectId, nint a3)
		{
			switch (Marshal.ReadByte(a3 + 4)) {
				case 16:
					// case 16: 交易状态更新
					PluginUi.Trade.SetTradeTarget(objectId);
					break;
				case 17:
					// case 17: 交易成功
					break;
				default:
					PluginLog.Information($"交易目标: {a1:X}, {objectId:X}, {a3:X}, {Marshal.ReadByte(a3 + 4)}");
					// case 7: 取消交易
					// case 1: 别人交易你
					// case 2: 发起交易

					// case 5: 最终确认
					break;
			}
			return this._tradeTargetHook!.Original(a1, objectId, a3);
		}

		[Signature("0F B7 42 06 4C 8B D1 44 8B 4A 10 4C 6B C0 38 41 80 BC 08", DetourName = nameof(DetourTradeOtherMoney))]
		private Hook<TradeOtherMoney>? _tradeOtherMoney;
		private delegate nint TradeOtherMoney(nint a1, nint a2);
		private nint DetourTradeOtherMoney(nint a1, nint a2)
		{
			PluginLog.Information($"交易对面出价: {a1:X}, {a2:X}, {(uint)Marshal.ReadInt32(a2 + 8)}");
			return this._tradeOtherMoney!.Original(a1, a2);
		}

		[Signature("3B 51 08 7D ?? 48 8B 41 20 48 63 D2 44 39 04 90", DetourName = nameof(DetourTradeCount))]
		private Hook<TradeCount>? _tradeCountHook;
		private delegate void TradeCount(nint a1, int a2, int a3);
		private void DetourTradeCount(nint a1, int a2, int a3)
		{
			PluginLog.Information($"交易 物品槽 数量: {a1:X}, {a2:X}, {a3}");
			this._tradeCountHook!.Original(a1, a2, a3);
		}
	}
}
