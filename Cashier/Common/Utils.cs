using Dalamud.Game.Text;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace TradeRecorder.Common
{
    public class Utils
    {
        public static bool DrawIconButton(FontAwesomeIcon icon, int? id = null, string? label = null)
        {
            if (id != null) { ImGui.PushID((int)id); }
            ImGui.PushFont(UiBuilder.IconFont);
            string buildStr;
            if (label != null)
            {
                buildStr = icon.ToIconString() + ("##" + label ?? "");
            }
            else
            {
                buildStr = icon.ToIconString();
            }
            bool _return = ImGui.Button(buildStr);

            ImGui.PopFont();
            if (id != null) { ImGui.PopID(); }
            return _return;
        }
        /// <summary>
        /// 获取背包某一格子的物品
        /// </summary>
        /// <param name="page">第几页，0起</param>
        /// <param name="index">序号，0起</param>
        /// <returns></returns>
        public unsafe static InventoryItem* GetInventoryItem(InventoryType page, int index)
        {
            return InventoryManager.Instance()->GetInventoryContainer(page)->GetInventorySlot(index);
        }

    }
}
