using FFXIVClientStructs.FFXIV.Client.Game;
using System.Runtime.InteropServices;

namespace TradeRecorder.Model.FFXIV
{
    [StructLayout(LayoutKind.Sequential, Size = 0x26)]
    public struct InventoryModifyHandler
    {
        public ushort Index;// 上行的InventoryModifyHandler序号，与下行的InventoryActionAck对应
        public byte unknown_02;
        public byte unknown_03;// 10
        public byte unknown_04;
        public byte unknown_05;// 03
        public byte unknown_06;
        public byte unknown_07;
        public byte unknown_08;
        public byte unknown_09;
        public byte unknown_0A;
        public byte unknown_0B;
        /// <summary>
        /// 道具来源的InventoryContainer
        /// </summary>
        public InventoryType Container;// 0C - 0D，0x07D5 交易栏，0x07D1 水晶 slot为itemId - 2
        public ushort Slot;// 10 - 11
        public byte unknown_12;
        public byte unknown_13;
        public uint Count;// 14 - 17 道具数量
        public byte unknown_18;
        public byte unknown_19;
        public byte unknown_1A;
        public byte unknown_1B;
        public byte unknown_1C;
        public byte unknown_1D;
        public byte unknown_1E;
        public byte unknown_1F;
        /// <summary>
        /// 移动到的InverntoryContainer
        /// </summary>
        public InventoryType Container2;// 20 - 23
        public ushort Slot2;// 24 - 25
    }
}
