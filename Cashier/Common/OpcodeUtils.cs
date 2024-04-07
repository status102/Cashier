using Dalamud.Game.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TradeRecorder.Model;

namespace TradeRecorder.Common
{
	public class OpcodeUtils
	{
		private static byte[] openWindowBytes = new byte[11] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		private static byte[] closeWindowBytes = new byte[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		private static HashSet<ushort> openWindow = new(), closeWindow = new();
		private static ushort windowOpcode = 0, tradeTargetOpcode = 0;
		private static Action<bool, ushort, ushort>? onFinish = null;
		public static void CaptureOpcode(Action<bool, ushort, ushort> onFinish)
		{
			openWindow = new();
			closeWindow = new();
			OpcodeUtils.onFinish = onFinish;
			tradeTargetOpcode = windowOpcode = 0;
			Svc.GameNetwork.NetworkMessage += CaptureOpcodeDelegate;
		}
		private static unsafe void CaptureOpcodeDelegate(IntPtr dataPtr, ushort opcode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
		{
			if (direction == NetworkMessageDirection.ZoneDown) {
				if (Marshal.ReadInt32(dataPtr) == 0) {
					closeWindow.Add(opcode);
					if (openWindow.Contains(opcode)) {
						windowOpcode = opcode;
					}
					Check();
				} else if (Marshal.ReadInt32(dataPtr) == 0x1000) {
					if (!closeWindow.Contains(opcode)) {
						openWindow.Add(opcode);
					} else {
						closeWindow.Remove(opcode);
					}
				}
				if (Marshal.ReadInt16(dataPtr, 4) == 0x0310 && Marshal.ReadByte(dataPtr, 43) == 0x10) {
					tradeTargetOpcode = opcode;
					Check();
				}
			}
		}
		private static void Check()
		{
			if (onFinish == null) {
				Svc.GameNetwork.NetworkMessage -= CaptureOpcodeDelegate;
				Svc.PluginLog.Warning("捕获opcode失败：回调为空");
				return;
			}
			if (windowOpcode != 0 && tradeTargetOpcode != 0) {
				Svc.GameNetwork.NetworkMessage -= CaptureOpcodeDelegate;
				onFinish?.Invoke(true, windowOpcode, tradeTargetOpcode);
				onFinish = null;
			}
		}
		public static void Cancel()
		{
			Svc.GameNetwork.NetworkMessage -= CaptureOpcodeDelegate;
			if (onFinish != null) {
				onFinish?.Invoke(false, 0, 0);
				onFinish = null;
			}
		}

		public static async Task<List<Opcodes>> GetOpcodesFromGitHub() { return await GetOpcodesFromGitHub(CancellationToken.None); }
		public static async Task<List<Opcodes>> GetOpcodesFromGitHub(CancellationToken cancellationToken)
		{
			var uriBuilder = new UriBuilder($"https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json");

			cancellationToken.ThrowIfCancellationRequested();

			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
			using var result = await client.GetAsync(uriBuilder.Uri, cancellationToken);

			if (result.StatusCode != HttpStatusCode.OK) { throw new HttpRequestException("Invalid status code " + result.StatusCode, null, result.StatusCode); }
			await using var responseStream = await result.Content.ReadAsStreamAsync(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();

			var parsedRes = await JsonSerializer.DeserializeAsync<List<Opcodes>>(responseStream, cancellationToken: cancellationToken);

			if (parsedRes == null) { throw new HttpRequestException("GitHub returned null response"); }

			return parsedRes;
		}
	}
}
