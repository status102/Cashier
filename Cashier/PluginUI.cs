using Cashier.Commons;
using Cashier.Windows;
using Dalamud.Game.Network;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Cashier
{
    public sealed unsafe class PluginUI : IDisposable
    {
        // 测试用opcode黑名单
        private readonly static int[] blackList = [673, 379, 521, 572, 113, 241, 280, 169, 504, 642, 911, 365];

        private readonly static Dictionary<long, IDalamudTextureWrap?> iconList = [];
        public History History { get; init; }
        public Trade Trade { get; init; }
        public Setting Setting { get; init; }
        public Main Main { get; init; }

        //public AtkArrayDataHolder* atkArrayDataHolder { get; init; } = null;

        public StreamWriter? networkMessageWriter;

        public unsafe PluginUI(Cashier cashier)
        {
            Trade = new(cashier);
            History = new(cashier);
            Setting = new(cashier);
            Main = new(cashier);

            //var atkArrayDataHolder = &Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
            //if (atkArrayDataHolder != null && atkArrayDataHolder->StringArrayCount > 0) { this.atkArrayDataHolder = atkArrayDataHolder; }
#if DEBUG
            Svc.GameNetwork.NetworkMessage += NetworkMessageDelegate;
#endif
        }

        public void Dispose()
        {
#if DEBUG
            Svc.GameNetwork.NetworkMessage -= NetworkMessageDelegate;
#endif
            Trade.Dispose();
            Setting.Dispose();
            History.Dispose();
            Main.Dispose();

            if (networkMessageWriter != null) {
                networkMessageWriter.Flush();
                networkMessageWriter.Close();
            }
            iconList.Values.ToList().ForEach(i => i?.Dispose());
        }

        public void Draw()
        {
            try {
                Trade?.Draw();// 有问题
            } catch (Exception e) {
                Svc.PluginLog.Warning("Trade.Draw出错\n" + e);
            }
            try {
                Main?.Draw();
            } catch (Exception e) {
                Svc.PluginLog.Warning("Main.Draw出错\n" + e);
            }
            try {
                History?.Draw();
            } catch (Exception e) {
                Svc.PluginLog.Warning("History.Draw出错\n" + e);
            }
            try {
                Setting?.Draw();
            } catch (Exception e) {
                Svc.PluginLog.Warning("Setting.Draw出错\n" + e);
            }
        }

        public unsafe void NetworkMessageDelegate(IntPtr dataPtr, ushort opcode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
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
                    stringBuilder.Append('-').Append(databyte.ToString("X"));
                }
                networkMessageWriter.WriteLine(stringBuilder.ToString());
                //networkMessageWriter.WriteLine(Encoding.UTF8.GetString(databyte));
            }
        }

        public static IDalamudTextureWrap? GetIcon(uint iconId, bool isHq = false)
        {
            if (iconId == 0) {
                return null;
            }
            long id = iconId;
            if (isHq) {
                id = -id;
            }
            if (iconList.TryGetValue(id, out var iconValue)) {
                return iconValue;
            }
            IDalamudTextureWrap? icon = Svc.TextureProvider.GetTextureFromGame(string.Format("ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex", iconId / 1000u, isHq ? "hq/" : "", iconId));
            if (icon == null) {
                return null;
            }
            iconList.Add(id, icon);
            return icon;
        }
    }
}
