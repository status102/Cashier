using Cashier.Commons;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Linq;
using System.Numerics;
using Addon = Cashier.Commons.Addon;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using TaskManager = ECommons.Automation.TaskManager;

namespace Cashier.Addons;

public unsafe class AddonTradeHelper
{
    private static TaskManager TaskManager => Cashier.TaskManager!;
    private static readonly Random Random = new(DateTime.Now.Millisecond);
    private static int RandomDelay => Random.Next(100, 300);
    public static readonly InventoryType[] InventoryTypes = [InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4];

    public static void TradeItem(uint itemId, uint count)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager is null) {
            Svc.Log.Error("InventoryManager is null");
            return;
        }
        var foundType = InventoryTypes.Where(i => inventoryManager->GetItemCountInContainer(itemId, i) != 0).ToList();
        if (foundType.Count == 0) {
            Svc.Log.Error("背包里没找到");
            return;
        }

        var container = inventoryManager->GetInventoryContainer(foundType.First());
        if (container == null) {
            Svc.Log.Error("container获取失败");
            return;
        }

        int? foundSlot = null;
        for (var i = 0; i < container->Size; i++) {
            var slot = container->GetInventorySlot(i);
            if (slot->ItemID == itemId) {
                foundSlot = i;
                break;
            }
        }
        if (foundSlot == null) {
            Svc.Log.Error("foundSlot失败");
            return;
        }


        var agentInventory = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory);
        if (agentInventory == null) {
            Svc.Log.Error("背包获取失败");
            return;
        }
        AgentInventoryContext.Instance()->OpenForItemSlot(foundType.First(), (int)foundSlot, agentInventory->AddonId);

        TaskManager.DelayNext(RandomDelay);
        TaskManager.Enqueue(() => Addon.TryClickContextMenuEntry("交易"));
        TaskManager.DelayNext(RandomDelay);
        TaskManager.Enqueue(() => Step.SetCount(count));
    }

    public static void TradeItem(InventoryType type, int slot, uint count)
    {
        var agentInventory = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory);
        if (agentInventory == null) {
            Svc.Log.Error("AgentModule.GetAgentByInternalId(AgentId.Inventory) is null");
            return;
        }
        AgentInventoryContext.Instance()->OpenForItemSlot(type, slot, agentInventory->AddonId);

        TaskManager.DelayNextImmediate(RandomDelay);
        TaskManager.EnqueueImmediate(() => Addon.TryClickContextMenuEntry("交易"));
        if (count > 1) {
            TaskManager.DelayNextImmediate(RandomDelay);
            TaskManager.EnqueueImmediate(() => Step.SetCount(count));
        }
    }

    /// <summary>
    /// 向某人申请交易
    /// </summary>
    /// <param name="playerName"></param>
    public static void RequestTrade(string playerName)
    {
        var objAddress = Svc.ObjectTable
            .FirstOrDefault(x => x.Name.TextValue == playerName
                && x.ObjectKind == ObjectKind.Player
                && IsDistanceEnough(x.Position)
                && x.IsTargetable)?.Address ?? nint.Zero;
        if (objAddress == nint.Zero) {
            Svc.Log.Warning("找不到目标" + playerName);
            return;
        }
        TargetSystem.Instance()->Target = (GameObject*)objAddress;
        TargetSystem.Instance()->OpenObjectInteraction((GameObject*)objAddress);

        TaskManager.DelayNext(50);
        TaskManager.Enqueue(() => Addon.TryClickContextMenuEntry("申请交易"));
        return;
    }

    /// <summary>
    /// 向某人申请交易
    /// </summary>
    /// <param name="playerName"></param>
    public static void RequestTrade(uint objectId)
    {
        var gameObject = Svc.ObjectTable.SearchById(objectId);
        if (gameObject == null) {
            Svc.Log.Info($"找不到目标,id:{objectId:X}");
            return;
        } else if (!IsDistanceEnough(gameObject.Position)) {
            Svc.Log.Info($"距离过远,id:{objectId:X}");
            return;
        }
        TargetSystem.Instance()->Target = (GameObject*)gameObject.Address;
        TargetSystem.Instance()->OpenObjectInteraction((GameObject*)gameObject.Address);

        TaskManager.DelayNextImmediate(50);
        TaskManager.EnqueueImmediate(() => Addon.TryClickContextMenuEntry("申请交易"));
        return;
    }

    /// <summary>
    /// 设置交易的金额，能突破100w上限，一眼挂
    /// </summary>
    /// <param name="money"></param>
    public static void SetGil(uint money)
    {
        TaskManager.EnqueueImmediate(Step.ClickMoney);
        TaskManager.DelayNextImmediate(50);
        TaskManager.EnqueueImmediate(() => Step.SetCount(money));
    }


    /// <summary>
    /// 主动取消交易
    /// </summary>
    public static void Cancel()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon)) {
            Svc.Log.Error("Trade为空");
        } else {
            Callback.Fire(addon, true, 1, 0);
        }
    }

    public static bool IsDistanceEnough(Vector3 pos2)
    {
        var pos = Svc.ClientState.LocalPlayer!.Position;
        return Math.Pow(pos.X - pos2.X, 2) + Math.Pow(pos.Z - pos2.Z, 2) < 16;
    }

    public static class Step
    {

        /// <summary>
        /// 点击交易窗己方金币栏
        /// </summary>
        public static void ClickMoney()
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var tradeAddon)) {
                Svc.Log.Error("Trade为空");
            } else {
                Callback.Fire(tradeAddon, true, 2, 0);
            }
        }

        public static void SetCount(uint count)
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("InputNumeric", out var inputNumeric)) {
                Svc.Log.Error("Input为空");
            } else {
                Callback.Fire(inputNumeric, true, count);
            }
        }

        /// <summary>
        /// 清除某一栏的道具
        /// </summary>
        /// <param name="slotIndex"></param>
        public static void ClearSlot(int slotIndex)
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon)) {
                Svc.Log.Error("Trade为空");
            } else {
                Callback.Fire(addon, true, 3, slotIndex);
            }
        }

        /// <summary>
        /// 第一次确认
        /// </summary>
        public static void PreCheck()
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon)) {
                Svc.Log.Error("Trade为空");
            } else {
                Callback.Fire(addon, true, 0, 0);
            }
        }

        public static void FinalCheck(bool confirm = true)
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectYesno", out var addon)) {
                Svc.Log.Error("SelectYesno为空");
            } else {
                Callback.Fire(addon, true, confirm ? 0 : 1);
            }
        }
    }
}
