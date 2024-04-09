using Dalamud.Memory;
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
	}
}
