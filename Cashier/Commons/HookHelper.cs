using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using System;
using System.Runtime.InteropServices;

namespace Cashier.Commons
{
	public class HookHelper : IDisposable
	{
		private bool isDisposed;

		private readonly Cashier cashier;

		public HookHelper(Cashier cashier)
		{
			this.cashier = cashier;
			Svc.GameInteropProvider.InitializeFromAttributes(this);

			//_resetSlotHook?.Enable();
			_setSlotItemIdHook?.Enable();
			_tradeStatusHook?.Enable();
			_tradeOtherMoney?.Enable();
			//_tradeCountHook?.Enable();
		}

		public void Dispose()
		{
			if (isDisposed)
				return;
			isDisposed = true;

			_tradeCountHook?.Dispose();
			_tradeOtherMoney?.Dispose();
			_tradeStatusHook?.Dispose();
			_resetSlotHook?.Dispose();
			_setSlotItemIdHook?.Dispose();
		}




		[Signature("40 53 48 83 EC 20 83 79 70 00 48 8B D9 74 36", DetourName = nameof(DetourResetSlot))]
		private Hook<ResetSlot>? _resetSlotHook;
		private delegate nint ResetSlot(nint a);
		private nint DetourResetSlot(nint a)
		{
			Svc.PluginLog.Information($"一个格子被清空: {a:X}");

			try {
				// your plugin logic goes here.
			} catch (Exception ex) {
				Svc.PluginLog.Error("An error occured when handling a macro save event." + ex.Message);
			}

			return _resetSlotHook!.Original(a);
		}

		[Signature("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 48 89 7C 24 20 41 56 48 83 EC 20 8B F2 41 8B E9 48 8B D9 45", DetourName = nameof(DetourSetSlotItemId))]
		private Hook<SetSlotItemId>? _setSlotItemIdHook;
		private delegate nint SetSlotItemId(nint a, uint itemId, nint a3, int a4, nint a5);
		private nint DetourSetSlotItemId(nint a, uint itemId, nint a3, int a4, nint a5)
		{
			// 未能读取到物品数量，暂时搁置
			Svc.PluginLog.Information($"一个格子设置: {a:X}, itemId:{itemId}, a3:{a3}, a4:{a4}, a5:{a5}");

			cashier.PluginUi.Trade.SetTradeSlotItem(a, (int)itemId);

			return _setSlotItemIdHook!.Original(a, itemId, a3, a4, a5);
		}

		[Signature("4C 8B DC 53 55 48 81 EC 68 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 40 01 00 00", DetourName = nameof(DetourTradeTarget))]
		private Hook<TradeTarget>? _tradeStatusHook;
		private delegate nint TradeTarget(nint a1, nint a2, nint a3);
		private nint DetourTradeTarget(nint a1, nint objectId, nint a3)
		{
			switch (Marshal.ReadByte(a3 + 4)) {
				case 1:
					// case 1: 别人交易你
					break;
				case 2:
					// case 2: 发起交易
					break;
				case 16:
					// case 16: 交易状态更新
					cashier.PluginUi.Trade.SetTradeTarget(objectId);
					break;
				case 7:
					// case 7: 取消交易
					break;
				case 17:
					// case 17: 交易成功
					break;
				default:
					Svc.PluginLog.Information($"交易目标: {a1:X}, {objectId:X}, {a3:X}, {Marshal.ReadByte(a3 + 4)}");


					// case 5: 最终确认
					break;
			}

			return _tradeStatusHook!.Original(a1, objectId, a3);
		}

		[Signature("0F B7 42 06 4C 8B D1 44 8B 4A 10 4C 6B C0 38 41 80 BC 08", DetourName = nameof(DetourTradeOtherMoney))]
		private Hook<TradeOtherMoney>? _tradeOtherMoney;
		private delegate nint TradeOtherMoney(nint a1, nint a2);
		private nint DetourTradeOtherMoney(nint a1, nint a2)
		{
			Svc.PluginLog.Information($"交易对面出价: {a1:X}, {a2:X}, {(uint)Marshal.ReadInt32(a2 + 8)}");
			return _tradeOtherMoney!.Original(a1, a2);
		}

		[Signature("3B 51 08 7D ?? 48 8B 41 20 48 63 D2 44 39 04 90", DetourName = nameof(DetourTradeCount))]
		private Hook<TradeCount>? _tradeCountHook;
		private delegate void TradeCount(nint a1, int a2, int a3);
		private void DetourTradeCount(nint a1, int a2, int a3)
		{
			Svc.PluginLog.Information($"交易 物品槽 数量: {a1:X}, {a2:X}, {a3}");
			_tradeCountHook!.Original(a1, a2, a3);
		}
	}
}
