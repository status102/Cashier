using Cashier.Windows;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using System;
using System.Runtime.InteropServices;

namespace Cashier.Commons;

public sealed class HookHelper : IDisposable
{
    private readonly Cashier _cashier;
    private bool _isDisposed;

    public delegate void TradeBegined(nint objectId);
    public TradeBegined? OnTradeBegined;
    public delegate void TradeEvent();
    public TradeEvent? OnTradeFinished;
    public TradeEvent? OnTradeCanceled;
    public TradeEvent? OnTradeFinalCheck;
    public delegate void TradeConfirmChanged(nint objectId, bool confirm);
    public TradeConfirmChanged? OnTradeConfirmChanged;
    public delegate void TradeMoneyChanged(uint money, bool isPlayer1);
    public TradeMoneyChanged? OnTradeMoneyChanged;
    public delegate void TradeItemSlotSet(nint a1, int itemId);
    public TradeItemSlotSet? OnSetTradeItemSlot;
    public delegate void TradeItemSlotClear(nint a1);
    public TradeItemSlotClear? OnClearTradeItemSlot;

    public HookHelper(Cashier cashier)
    {
        _cashier = cashier;
        Svc.GameInteropProvider.InitializeFromAttributes(this);

        _tradeStatusUpdateHook?.Enable();
        _tradeRequestHook?.Enable();

        _setSlotItemIdHook?.Enable();
        _setSlotItemCount2Hook?.Enable();
        _resetSlotHook?.Enable();
        _tradeMyMoney?.Enable();
        _tradeOtherMoney?.Enable();
        //_tradeCountHook?.Enable();
        //_executeCommandHook?.Enable();
    }

    public void Dispose()
    {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;

        _tradeStatusUpdateHook?.Dispose();
        _tradeRequestHook?.Dispose();

        _setSlotItemIdHook?.Dispose();
        _setSlotItemCount2Hook?.Dispose();
        _resetSlotHook?.Dispose();
        _tradeMyMoney?.Dispose();
        _tradeOtherMoney?.Dispose();
        _tradeCountHook?.Dispose();
        _executeCommandHook?.Dispose();
    }


    [Signature("40 53 48 83 EC 20 83 79 70 00 48 8B D9 74 36", DetourName = nameof(DetourResetSlot))]
    private Hook<ResetSlot>? _resetSlotHook;
    private delegate nint ResetSlot(nint a);
    private nint DetourResetSlot(nint a)
    {
        //一个格子被清空
        OnClearTradeItemSlot?.Invoke(a);
        return _resetSlotHook!.Original(a);
    }

    [Signature("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 48 89 7C 24 20 41 56 48 83 EC 20 8B F2 41 8B E9 48 8B D9 45", DetourName = nameof(DetourSetSlotItemId))]
    private Hook<SetSlotItemId>? _setSlotItemIdHook;
    private delegate nint SetSlotItemId(nint a, uint itemId, nint a3, int a4, nint a5);
    private nint DetourSetSlotItemId(nint a, uint itemId, nint a3, int a4, nint a5)
    {
        // 未能读取到物品数量，暂时搁置
        //Svc.PluginLog.Debug($"一个格子设置: {a:X}, itemId:{itemId}, a3:{a3}, a4:{a4}, a5:{a5}");
        OnSetTradeItemSlot?.Invoke(a, (int)itemId);
        return _setSlotItemIdHook!.Original(a, itemId, a3, a4, a5);
    }

    #region 交易目标及状态

    [Signature("48 89 6C 24 ?? 57 41 56 41 57 48 83 EC ?? 48 8B E9 44 8B FA", DetourName = nameof(DetourTradeRequest))]
    private Hook<TradeRequest>? _tradeRequestHook;
    private delegate nint TradeRequest(nint a1, nint a2);
    public nint DetourTradeRequest(nint a1, nint objectId)
    {
        // a1: InventoryManager.Instance / baseAddress + 0x21F16C0
        // 正常0
        // 对无法发起交易的对象、超距，2 ||(48|1310) 无法向“忙碌中”状态的玩家申请交易。| 距离太远。
        // 现在无法进行交易，19 || /(无法战斗|制作|采集)状态下无法进行该操作。/
        var result = _tradeRequestHook!.Original(a1, objectId);
        if (result == 0) {
            OnTradeBegined?.Invoke(objectId);
        }
        return result;
    }

    [Signature("4C 8B DC 53 55 48 81 EC 68 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 40 01 00 00", DetourName = nameof(DetourTradeStatusUpdate))]
    private Hook<TradeStatusUpdate>? _tradeStatusUpdateHook;
    private delegate nint TradeStatusUpdate(nint a1, nint a2, nint a3);
    private nint DetourTradeStatusUpdate(nint a1, nint a2, nint a3)
    {
        // a2 为ObjectId的交易对象，但是似乎源码都用的a3+40
#if DEBUG
        Svc.Log.Debug($"交易状态: {a1:X}, {a2:X}, {a3:X}, {Marshal.ReadByte(a3 + 4)}");
#endif
        switch (Marshal.ReadByte(a3 + 4)) {
            case 1:
                // 别人交易你
                OnTradeBegined?.Invoke(Marshal.ReadInt32(a3 + 40));
                break;
            case 2:
                // 发起交易
                //OnTradeBegined?.Invoke();
                break;
            case 16:
                // 交易状态更新
                var a3_5 = Marshal.ReadByte(a3 + 5);
#if DEBUG
                Svc.Log.Debug($"交易状态0x10: {a1:X}, {a2:X}, {a3:X}, {a3_5}");
#endif
                switch (a3_5) {
                    case 3:
                        OnTradeConfirmChanged?.Invoke(Marshal.ReadInt32(a3 + 40), false);
                        break;
                    case 4:
                    case 5:
                        // 先确认条件的一边会产生一个a=4，两边都确认后发两个a=5
                        // 最终确认先确认的产生一个a=6，两边都确认后发两个a=1
                        OnTradeConfirmChanged?.Invoke(Marshal.ReadInt32(a3 + 40), true);
                        break;
                }
                break;
            case 5:
                // 最终确认
                OnTradeFinalCheck?.Invoke();
                break;
            case 7:
                // 取消交易
                OnTradeCanceled?.Invoke();
                break;
            case 17:
                // 交易成功
                OnTradeFinished?.Invoke();
                break;
            default:
#if DEBUG
                Svc.Log.Debug($"交易状态: {a1:X}, {a2:X}, {a3:X}, {Marshal.ReadByte(a3 + 4)}");
#endif
                break;
        }

        return _tradeStatusUpdateHook!.Original(a1, a2, a3);
    }

    #endregion

    #region 交易出价

    [Signature("0F B7 42 06 4C 8B D1 44 8B 4A 10 4C 6B C0 38 41 80 BC 08", DetourName = nameof(DetourTradeOtherMoney))]
    private Hook<TradeOtherMoney>? _tradeOtherMoney;
    private delegate nint TradeOtherMoney(nint a1, nint a2);
    private nint DetourTradeOtherMoney(nint a1, nint a2)
    {
        OnTradeMoneyChanged?.Invoke((uint)Marshal.ReadInt32(a2 + 8), false);
        return _tradeOtherMoney!.Original(a1, a2);
    }

    [Signature("48 89 5C 24 08 57 48 83 EC 20 8B DA 48 8B F9 E8 ?? ?? ?? ?? 3B D8 76 ?? B8 08 00 00 00 48 8B 5C 24 30 48 83 C4 20 5F", DetourName = nameof(DetourTradeMyMoney))]
    private Hook<TradeMyMoney>? _tradeMyMoney;
    private delegate nint TradeMyMoney(nint a1, uint a2);
    public nint DetourTradeMyMoney(nint a1, uint a2)
    {
        // 正常0
        // 超出自身上限8
        OnTradeMoneyChanged?.Invoke(a2, true);
        return _tradeMyMoney!.Original(a1, a2);
    }
    #endregion

    #region 测试中

    [Signature("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 48 89 7C 24 20 41 56 48 83 EC 20 33 FF 49 8B E8", DetourName = nameof(DetourSetSlotItemCount2))]
    private Hook<SetSlotItemCount2>? _setSlotItemCount2Hook;
    private delegate nint SetSlotItemCount2(nint a1, nint a2, nint a3, int a4);
    private nint DetourSetSlotItemCount2(nint a1, nint a2, nint a3, int a4)
    {
#if DEBUG
        if (((Marshal.ReadByte(a3) & 0x0F) != 0x00) && Marshal.ReadInt32(a3 + 8) > 0) {
            Svc.Log.Debug($"格子数量设置: {a1:X}, a2:{a2:X}, a3:{a3}, a4:{a4}, count:{(uint)Marshal.ReadInt32(a3 + 8)}, a1+16:{a1 + 16:X}, a1+16+12:{a1 + 16 + 12:X}, *(a1+16):{Marshal.ReadInt64(a1 + 16):X}");
        }
#endif
        return _setSlotItemCount2Hook!.Original(a1, a2, a3, a4);
    }

    [Signature("3B 51 08 7D ?? 48 8B 41 20 48 63 D2 44 39 04 90", DetourName = nameof(DetourTradeCount))]
    private readonly Hook<TradeCount>? _tradeCountHook;
    private delegate void TradeCount(nint a1, int a2, int a3);
    private void DetourTradeCount(nint a1, int a2, int a3)
    {
#if DEBUG
        Svc.Log.Information($"交易 物品槽 数量: {a1:X}, {a2:X}, {a3}");
#endif
        _tradeCountHook!.Original(a1, a2, a3);
    }

    private delegate nint ExecuteCommandDelegate(int command, int a2, int a3, int a4, int a5);

    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B E9 41 8B D9 48 8B 0D ?? ?? ?? ?? 41 8B F8 8B F2", DetourName = nameof(DetourExecuteCommand))]
    private readonly Hook<ExecuteCommandDelegate>? _executeCommandHook;
    private nint DetourExecuteCommand(int command, int a2, int a3, int a4, int a5)
    {
        Svc.Log.Debug($"ExecuteCommand: {command:X}, {a2:X}, {a3:X}, {a4:X}, {a5:X}");
        return _executeCommandHook!.Original(command, a2, a3, a4, a5);
    }
    #endregion
}
