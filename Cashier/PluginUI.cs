using Cashier.Commons;
using Dalamud.Game.Network;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static Dalamud.Plugin.Services.ITextureProvider;

namespace Cashier
{
    public sealed unsafe class PluginUI : IDisposable
    {
        // 测试用opcode黑名单
        private readonly static int[] blackList = [673, 379, 521, 572, 113, 241, 280, 169, 504, 642, 911, 365];

        private readonly static Dictionary<long, IDalamudTextureWrap?> iconList = [];

        //public AtkArrayDataHolder* atkArrayDataHolder { get; init; } = null;

        public StreamWriter? networkMessageWriter;

        public unsafe PluginUI(Cashier cashier)
        {
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

            if (networkMessageWriter != null) {
                networkMessageWriter.Flush();
                networkMessageWriter.Close();
            }
            iconList.Values.ToList().ForEach(i => i?.Dispose());
        }

        public void Draw()
        {
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

        public static IDalamudTextureWrap? GetIcon(uint iconId, bool quality = false)
        {
            if (iconId == 0) {
                return null;
            }

            long cacheId = quality ? -iconId : iconId;
            if (iconList.TryGetValue(cacheId, out var iconValue)) {
                return iconValue;
            }

            var flag = IconFlags.HiRes;
            if (quality) {
                flag |= IconFlags.ItemHighQuality;
            }

            if (Svc.TextureProvider.GetIcon(iconId, flag) is { } icon) {
                iconList.Add(cacheId, icon);
                return icon;
            }

            return null;
        }
    }
}
