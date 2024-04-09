using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;

namespace Cashier
{
	/// <summary>
	/// 具体大小未知
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct AgentTrade
	{

		[FieldOffset(0)]
		public AgentInterface AgentInterface;

		 
		[FieldOffset(48)]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
		public Slot[] TradeSlot;

		[StructLayout(LayoutKind.Explicit, Size = 136)]
		public struct Slot
		{
			[FieldOffset(0)]
			public nint Unknown;

			[FieldOffset(112)]
			public int ItemId;
		}
	}
}
