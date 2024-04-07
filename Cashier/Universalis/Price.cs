using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TradeRecorder.Common;

namespace TradeRecorder.Universalis
{
    public class Price
	{
		public const string format = "yyyy-MM-dd HH:mm:ss";
		private static readonly Dictionary<uint, string> cnWorldDC = new();
		/// <summary>
		/// 最低查询间隔，以秒为单位
		/// </summary>
		private const int Check_Delay = 60 * 60;
		private static readonly Dictionary<uint, ItemPrice> items = new();

		static Price() {
			#region 添加CN服务器对应dc
			cnWorldDC.Add(1167, "LuXingNiao");
			cnWorldDC.Add(1081, "LuXingNiao");
			cnWorldDC.Add(1042, "LuXingNiao");
			cnWorldDC.Add(1044, "LuXingNiao");
			cnWorldDC.Add(1060, "LuXingNiao");
			cnWorldDC.Add(1173, "LuXingNiao");
			cnWorldDC.Add(1174, "LuXingNiao");
			cnWorldDC.Add(1175, "LuXingNiao");
			cnWorldDC.Add(1172, "MoGuLi");
			cnWorldDC.Add(1076, "MoGuLi");
			cnWorldDC.Add(1171, "MoGuLi");
			cnWorldDC.Add(1170, "MoGuLi");
			cnWorldDC.Add(1113, "MoGuLi");
			cnWorldDC.Add(1121, "MoGuLi");
			cnWorldDC.Add(1166, "MoGuLi");
			cnWorldDC.Add(1176, "MoGuLi");
			cnWorldDC.Add(1043, "MaoXiaoPang");
			cnWorldDC.Add(1169, "MaoXiaoPang");
			cnWorldDC.Add(1106, "MaoXiaoPang");
			cnWorldDC.Add(1045, "MaoXiaoPang");
			cnWorldDC.Add(1177, "MaoXiaoPang");
			cnWorldDC.Add(1178, "MaoXiaoPang");
			cnWorldDC.Add(1179, "MaoXiaoPang");
			cnWorldDC.Add(1192, "DouDouChai");
			cnWorldDC.Add(1183, "DouDouChai");
			cnWorldDC.Add(1180, "DouDouChai");
			cnWorldDC.Add(1186, "DouDouChai");
			cnWorldDC.Add(1193, "DouDouChai");
			cnWorldDC.Add(1068, "DouDouChai");
			cnWorldDC.Add(1064, "DouDouChai");
			cnWorldDC.Add(1187, "DouDouChai");
			#endregion
		}
		/// <summary>
		/// 获取最便宜的价格
		/// </summary>
		/// <param name="itemId">物品id</param>
		/// <returns></returns>
		public static ItemPrice GetItem(uint itemId) {
			if (!items.ContainsKey(itemId)) {
				items.Add(itemId, new ItemPrice(itemId));
			}
			return items[itemId];
		}

		public static string GetDcName(uint serverId) {
			if (cnWorldDC.ContainsKey(serverId)) {
				return cnWorldDC[serverId];
			} else {
				var world = Svc.DataManager.GetExcelSheet<World>()?.FirstOrDefault(i => i.RowId == serverId);
				if (world != null) {
					return world.DataCenter.Value?.Name ?? string.Empty;
				}
			}
			return string.Empty;
		}
		public class ItemPrice
		{
			private uint itemId { get; init; }
			/// <summary>
			/// 最低价服
			/// </summary>
			private string minPriceServerName = string.Empty;
			/// <summary>
			/// 最低价
			/// </summary>
			private int minPriceNQ = int.MinValue;
			private int minPriceHQ = int.MinValue;
			/// <summary>
			/// 最低价数据的上传时间，毫秒
			/// </summary>
			private long updateTime = 0;
			private string minPriceServer = string.Empty;

			private string lastCheckDc = string.Empty;
			/// <summary>
			/// 最近查询UTC时间，秒为单位，查询失败时为-1
			/// </summary>
			private long lastCheckTime = 0;
			/// <summary>
			/// 是否能在市场出售
			/// </summary>
			public readonly bool Marketable;
			public ItemPrice(uint itemId) {
				this.itemId = itemId;
				var item = Svc.DataManager.GetExcelSheet<Item>()?.FirstOrDefault(i => i.RowId == itemId);
				if (item != null) {
					Marketable = item.ItemSearchCategory.Row != 0;
				} else { Marketable = false; }
			}

			/// <summary>
			/// 获取所处大区的该物品最低价
			/// </summary>
			/// <param name="serverId">所在服务器id</param>
			/// <returns>(nq价格，hq价格，最低价服务器，价格上传时间)</returns>
			public (int, int, string, long) GetMinPrice(uint serverId) {
				if (!Marketable) { return (0, 0, "无法在市场出售", 0); }
				if ((DateTimeOffset.Now.ToUnixTimeSeconds() - lastCheckTime) > Check_Delay || GetDcName(serverId) != lastCheckDc) {
					lastCheckTime = DateTimeOffset.Now.ToUnixTimeSeconds();
					Task.Run(() => Update(serverId));
				}
				return (minPriceNQ, minPriceHQ, minPriceServer, updateTime);
			}
			public async void Update(uint serverId) {
				var dcName = GetDcName(serverId);

				try {
					var price = await API.UniversalisClient.GetMarketData(dcName, itemId, CancellationToken.None);
					if (price?.itemID != itemId) {
						updateTime = 0;
						minPriceNQ = -1;
						minPriceHQ = -1;
						minPriceServer = string.Empty;
					} else {
						updateTime = price.lastUploadTime;
						minPriceNQ = price.minPriceNQ;
						minPriceHQ = price.minPriceHQ;
						minPriceServer = price.listings?.FirstOrDefault()?.worldName ?? "";
					}
				} catch (HttpRequestException e) {
					Svc.PluginLog.Error("获取价格失败：" + e.ToString());
					minPriceServer = "获取失败：网络错误";
					updateTime = 0;
					minPriceNQ = 0;
					minPriceHQ = 0;
				}
				lastCheckDc = dcName;
			}
		}
	}
}
