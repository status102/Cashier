using Dalamud.Game.Network;
using Dalamud.Interface.Internal;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TradeRecorder.Common;
using TradeRecorder.Window;

namespace TradeRecorder
{
    public unsafe class PluginUI : IDisposable
	{
		public readonly static char[] intToHex = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		private Configuration config;
		private readonly static int[] blackList = { 673, 379, 521, 572, 113, 241, 280, 169, 504, 642, 911, 365 };

		private readonly static Dictionary<uint, IDalamudTextureWrap?> iconList = new();
		private readonly static Dictionary<uint, IDalamudTextureWrap?> hqIconList = new();
		public History History { get; init; }
		public Trade Trade { get; init; }
		public Setting Setting { get; init; }

		//public AtkArrayDataHolder* atkArrayDataHolder { get; init; } = null;

		public StreamWriter? networkMessageWriter;

		public unsafe PluginUI(TradeRecorder TradeRecorder, Configuration configuration) {
			this.config = configuration;
			Trade = new(TradeRecorder);
			History = new(TradeRecorder);
			Setting = new(TradeRecorder);

			//var atkArrayDataHolder = &Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
			//if (atkArrayDataHolder != null && atkArrayDataHolder->StringArrayCount > 0) { this.atkArrayDataHolder = atkArrayDataHolder; }
#if DEBUG
			Svc.GameNetwork.NetworkMessage += NetworkMessageDelegate;
#endif
		}

		public void Dispose() {
#if DEBUG
			Svc.GameNetwork.NetworkMessage -= NetworkMessageDelegate;
#endif
			Trade.Dispose();
			Setting.Dispose();
			History.Dispose();
			if (networkMessageWriter != null) {
				networkMessageWriter.Flush();
				networkMessageWriter.Close();
			}
			foreach (TextureWrap? icon in iconList.Values) { icon?.Dispose(); }
			foreach (TextureWrap? icon in hqIconList.Values) { icon?.Dispose(); }
		}

		public void Draw() {
			Trade.Draw();
			History.Draw();
			Setting.Draw();
		}

		public unsafe void NetworkMessageDelegate(IntPtr dataPtr, ushort opcode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
			if (networkMessageWriter != null) {
				StringBuilder stringBuilder = new();
				byte databyte;
				stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff "));
				int index = Marshal.ReadByte(dataPtr, 36);
				int codeint56 = Marshal.ReadInt32(dataPtr, 56);
				ushort codeShort56 = (ushort)Marshal.ReadInt16(dataPtr, 56);
				ushort codeShort64 = (ushort)Marshal.ReadInt16(dataPtr, 64);
				ushort codeShort72 = (ushort)Marshal.ReadInt16(dataPtr, 72);

				stringBuilder.Append(String.Format("OpCode：{0:}，[{1:}->{2:}]", opcode, sourceActorId, targetActorId));
				stringBuilder.Append(string.Format("(index：{0:}, id(int56)：{1:}, id(short56)：{2:}, id(short64):{3:}, id(short64)：{4:})", index, codeint56, codeShort56, codeShort64, codeShort72));


				stringBuilder.Append("： ");
				for (int i = 0; i < 200; i++) {
					databyte = Marshal.ReadByte(dataPtr, i);
					stringBuilder.Append('-').Append(intToHex[databyte / 16]).Append(intToHex[databyte % 16]);
				}
				networkMessageWriter.WriteLine(stringBuilder.ToString());
				//networkMessageWriter.WriteLine(Encoding.UTF8.GetString(databyte));
			}
		}

		public static IDalamudTextureWrap? GetIcon(uint iconId, bool isHq = false) {
			if (iconId == 0) { return null; }
			if (!isHq && iconList.TryGetValue(iconId, out var nqicon)) { return nqicon; }
			if (isHq && hqIconList.TryGetValue(iconId, out var hqIcon)) { return hqIcon; }
			IDalamudTextureWrap? icon = GetIconStr(iconId, isHq);
			if (icon == null) { return null; }
			if (isHq) {
				hqIconList.Add(iconId, icon);
			} else {
				iconList.Add(iconId, icon);
			}
			return icon;
		}
		private static IDalamudTextureWrap? GetIconStr(uint iconId, bool isHQ) {
			//"ui/icon/{0:D3}000/{1}{2:D6}.tex";
			return Svc.TextureProvider.GetTextureFromGame(string.Format("ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex", iconId / 1000u, isHQ ? "hq/" : "", iconId));
		}
	}
}
