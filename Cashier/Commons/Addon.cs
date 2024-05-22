using Dalamud.Memory;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace Cashier.Commons
{
	public unsafe class Addon
	{
		/// <summary>
		/// Try finding the index of specific ContextMenu addon entry by the text given.
		/// </summary>
		/// <param name="addon"></param>
		/// <param name="text"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static bool TryScanContextMenuText(AtkUnitBase* addon, string text, out int index)
		{
			index = -1;
			if (addon == null) {
				return false;
			}

			var entryCount = addon->AtkValues[0].UInt;
			if (entryCount == 0) {
				return false;
			}

			for (var i = 0; i < entryCount; i++) {
				var currentString = MemoryHelper.ReadStringNullTerminated((nint)addon->AtkValues[i + 7].String);
				if (!currentString.Contains(text, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				index = i;
				return true;
			}

			return false;
		}

		public static bool TryClickContextMenuEntry(string text)
		{
			if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContextMenu", out var addon)) {
				Svc.PluginLog.Error("获取ContextMenu失败");
				return false;
			}
			if (!TryScanContextMenuText(addon, text, out var index)) {
				Svc.PluginLog.Error($"ContextMenu内未找到包含[{text}]的选项，关闭ContextMenu");
				addon->FireCloseCallback();
				addon->Close(true);
				return false;
			}
			Callback.Fire(addon, true, 0, index, 0U, 0, 0);
			return true;
		}

		public static bool TryClickContextMenuIndex(int index)
		{
			if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContextMenu", out var addon)) {
				Svc.PluginLog.Error("获取ContextMenu失败");
				return false;
			}
			if (index < 0) {
				Svc.PluginLog.Error("index不能小于0");
				return false;
			}
			if (index >= addon->AtkValues[0].UInt) {
				Svc.PluginLog.Error("index超出范围");
				return false;
			}
			Callback.Fire(addon, true, 0, index, 0U, 0, 0);
			return true;
		}
	}
}
