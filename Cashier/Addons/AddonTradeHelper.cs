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
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using TaskManager = ECommons.Automation.TaskManager;

namespace Cashier.Addons
{
    public unsafe class AddonTradeHelper
    {
        private static TaskManager TaskManager => Cashier.TaskManager!;
        public static void TradeItem(uint itemId, int count)
        {
            var agent = AgentInventoryContext.Instance();
            if (agent == null) {
                Svc.PluginLog.Error("获取InventoryContext失败");
                return;
            }
            var inventoryManager = InventoryManager.Instance();
            if (inventoryManager == null) {
                Svc.PluginLog.Error("获取InventoryManager失败");
                return;
            }
            InventoryType[] InventoryTypes = [
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4
            ];
            var foundType = InventoryTypes.Where(i => inventoryManager->GetItemCountInContainer(itemId, i) != 0).ToList();
            if (foundType.Count == 0) {
                Svc.PluginLog.Error("背包里没找到");
                return;
            }
            var agentInventory = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory);
            if (agentInventory == null) {
                Svc.PluginLog.Error("背包获取失败");
                return;
            }

            var container = inventoryManager->GetInventoryContainer(foundType.First());
            if (container == null) {
                Svc.PluginLog.Error("container获取失败");
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
                Svc.PluginLog.Error("foundSlot失败");
                return;
            }

            agent->OpenForItemSlot(foundType.First(), (int)foundSlot, agentInventory->AddonId);

            TaskManager.DelayNext(50);
            TaskManager.Enqueue(() => Addon.TryClickContextMenuEntry("交易"));
            TaskManager.DelayNext(50);
            TaskManager.Enqueue(() => SetCount(count));
        }

        /// <summary>
        /// 设置交易的金额，能突破100w上限，一眼挂
        /// </summary>
        /// <param name="money"></param>
        public static void TradeGil(int money)
        {
            TaskManager.Enqueue(() =>
            {
                var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Trade");

                if (contextMenu is null) {
                    Svc.PluginLog.Error("Trade为空");
                    return;
                }

                Callback.Fire(contextMenu, true, 2, 0);
            });
            TaskManager.DelayNext(50);
            TaskManager.Enqueue(() => SetCount(money));
        }

        private static void SetCount(int count)
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("InputNumeric", out var inputNumeric)) {
                Svc.PluginLog.Error("Input为空");
            } else {
                Callback.Fire(inputNumeric, true, count);
            }
        }

        /// <summary>
        /// 向某人申请交易
        /// </summary>
        /// <param name="name"></param>
        public static void RequestTrade(string name)
        {
            var myPostion = Svc.ClientState.LocalPlayer!.Position;
            var distance = (Vector3 pos) =>
            {
                return Math.Pow(pos.X - myPostion.X, 2) + Math.Pow(pos.Z - myPostion.Z, 2) < 16;
            };
            var objAddress = Svc.ObjectTable
                .FirstOrDefault(x => x.Name.TextValue == name
                && x.ObjectKind == ObjectKind.Player
                && distance(x.Position)
                && x.IsTargetable)?.Address ?? nint.Zero;
            if (objAddress == nint.Zero) {
                Svc.ChatGui.Print("找不到目标" + name);
                return;
            }
            TargetSystem.Instance()->Target = (GameObject*)objAddress;
            TargetSystem.Instance()->OpenObjectInteraction((GameObject*)objAddress);

            TaskManager.DelayNext(50);
            TaskManager.Enqueue(() => Addon.TryClickContextMenuEntry("申请交易"));
            return;
        }

        /// <summary>
        /// 主动取消交易
        /// </summary>
        public static void CancelTrade()
        {
            TaskManager.Enqueue(() =>
            {
                if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon)) {
                    Svc.PluginLog.Error("Trade为空");
                } else {
                    Callback.Fire(addon, true, 1, 0);
                }
            });
        }

        public static void ClearTradeSlot(int slotIndex)
        {
            TaskManager.Enqueue(() =>
            {
                if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon)) {
                    Svc.PluginLog.Error("Trade为空");
                } else {
                    Callback.Fire(addon, true, 3, slotIndex);
                }
            });
        }

        public static void ConfirmTradeFirst()
        {
            TaskManager.Enqueue(() =>
            {
                if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var tradeAddon)) {
                    Svc.PluginLog.Error("Trade为空");
                } else {
                    Callback.Fire(tradeAddon, true, 0, 0);
                }
            });
        }
    }
}
