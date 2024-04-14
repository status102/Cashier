using Cashier.Commons;
using Cashier.Model;
using Dalamud.Interface.ImGuiFileDialog;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Cashier.Windows
{
    public class History : IWindow
    {
        /// <summary>
        /// 导出交易记录表的列名
        /// </summary>
        private static readonly string[] Title = ["时间", "交易状态", "交易对象", "支付金额", "接收金额", "支付物品", "接收物品"];
        private static readonly Vector2 Img_Size = new(26, 26);
        /// <summary>
        /// historyList被修改
        /// </summary>
        private bool isHistoryChanged = false;
        private FileDialog? fileDialog;
        private List<TradeHistory> historyList = [];
        private HashSet<string> historyTargetSet = [];
        private List<TradeHistory> showList = [];

        private readonly Cashier tradeRecorder;
        private bool visible = false;
        private string? target = null;

        public void Draw()
        {
            if (!visible) {
                if (isHistoryChanged) {
                    isHistoryChanged = false;
                    Task.Run(() => { SaveHistory(); ReadHistory(); });
                }
                return;
            }
            ImGui.SetNextWindowSize(new Vector2(600, 800), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("交易历史记录", ref visible, ImGuiWindowFlags.NoScrollbar)) {
                if (target == null) { if (ImGui.Button("全部清除")) { ClearHistory(); } } else {
                    if (ImGui.Button("清除当前目标")) {
                        ClearHistory(target);
                        ShowHistory();
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("导出到csv")) {
                    fileDialog = new FileDialog("save", "导出到csv", ".csv", tradeRecorder.PluginInterface.ConfigDirectory.FullName, "output.csv", "", 1, false, ImGuiFileDialogFlags.None);
                    fileDialog.Show();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(300);
                if (ImGui.BeginCombo("筛选目标", target ?? string.Empty)) {
                    if (ImGui.Selectable(" ", target == null)) {
                        if (target != null) {
                            ShowHistory();
                        }
                    }
                    foreach (var targetName in historyTargetSet) {
                        var isSelected = (target ?? string.Empty) == targetName;
                        if (ImGui.Selectable(targetName, isSelected)) {
                            if (!isSelected) {
                                ShowHistory(targetName);
                            }
                        }
                    }
                    ImGui.EndCombo();
                }
                if (ImGui.BeginChild("##历史清单")) {
                    for (int index = 0; index < showList.Count; index++) {
                        DrawHistory(index, showList[index]);
                    }
                }
            }

            // 判断文件保存框是否存在
            if (fileDialog != null && fileDialog.Draw()) {
                fileDialog.Hide();
                if (fileDialog.GetIsOk()) {
                    var resultList = fileDialog.GetResults();
                    if (resultList.Count == 1) {
                        var savePath = resultList[0].Trim();
                        if (!savePath.EndsWith(".csv")) { savePath += ".csv"; }
                        ExportHistory(savePath);
                    }
                }
            }

        }

        public void ShowHistory(string? target = null)
        {
            if (!visible) {
                visible = true;
            } else {
                if (this.target == target) {
                    visible = false;
                    return;
                }
            }
            if (target == null) {
                this.target = null;
                showList = historyList;
            } else {
                this.target = target;
                showList = historyList.Where(i => i.Target == this.target).ToList();
            }
        }

        /// <summary>
        /// 绘制单个交易
        /// </summary>
        /// <param name="index"></param>
        /// <param name="tradeItem"></param>
        private void DrawHistory(int index, TradeHistory tradeItem)
        {
            var title = $"{index + 1}:  {tradeItem.Time}  <{tradeItem.Target}>";
            if (!tradeItem.Status) {
                title += "  (取消)";
            }
            var expansion = ImGui.CollapsingHeader(title.ToString(), ref tradeItem.visible);
            // 如果处于显示状态，则绘制本次交易的净金币进出
            if (tradeItem.visible) {
                ImGui.SameLine(ImGui.GetColumnWidth() - 130);
                var get = (int)tradeItem.ReceiveGil - (int)tradeItem.GiveGil;
                ImGui.TextUnformatted($"{(get > 0 ? "+" : "")}{get:#,#}");
            }
            if (expansion) {
                if (ImGui.BeginTable($"histroy-{index}", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg)) {
                    ImGui.TableSetupColumn("支出");
                    ImGui.TableSetupColumn("收入");
                    ImGui.TableHeadersRow();

                    // 绘制本次交易物品
                    for (int i = 0; i < Math.Max(tradeItem.giveItemArray.Length, tradeItem.receiveItemArray.Length); i++) {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        if (tradeItem.giveItemArray.Length > i) {
                            var icon = tradeItem.giveItemArray[i].Icon;
                            if (icon != null) {
                                ImGui.Image(icon.ImGuiHandle, Img_Size);
                                ImGui.SameLine();
                            }
                            ImGui.TextUnformatted(tradeItem.giveItemArray[i].ToShowString());
                        }

                        ImGui.TableNextColumn();
                        if (tradeItem.receiveItemArray.Length > i) {
                            var icon = tradeItem.receiveItemArray[i].Icon;
                            if (icon != null) {
                                ImGui.Image(icon.ImGuiHandle, Img_Size);
                                ImGui.SameLine();
                            }
                            ImGui.TextUnformatted(tradeItem.receiveItemArray[i].ToShowString());
                        }

                    }

                    // 如果本次交易有金币进出，则单独显示一行金币
                    if (tradeItem.GiveGil > 0 || tradeItem.ReceiveGil > 0) {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if (tradeItem.GiveGil > 0) {
                            ImGui.TextUnformatted($"金币: {tradeItem.GiveGil:#,0}");
                        }

                        ImGui.TableNextColumn();
                        if (tradeItem.ReceiveGil > 0) {
                            ImGui.TextUnformatted($"金币: {tradeItem.ReceiveGil:#,0}");
                        }
                    }

                    ImGui.EndTable();
                }
            }
            // 有记录需要删除
            if (!tradeItem.visible) {
                isHistoryChanged = true;
            }
        }

        public void AddHistory(bool status, string target, uint[] gil, TradeItem[][] items)
        {
            if (Svc.ClientState.LocalPlayer == null) {
                Chat.PrintError("历史记录追加失败，Player为空");
                return;
            }
            var giveList = items[0].Select(i => new TradeHistory.HistoryItem(i.IconId, i.Name ?? "<Unknown>", i.Count, i.Quality)).ToArray();
            var receiviList = items[1].Select(i => new TradeHistory.HistoryItem(i.IconId, i.Name ?? "<Unknown>", i.Count, i.Quality)).ToArray();

            TradeHistory tradeHistory = new()
            {
                Status = status,
                Target = target,
                GiveGil = gil[0],
                ReceiveGil = gil[1],
                giveItemArray = giveList,
                receiveItemArray = receiviList
            };
            historyList.Add(tradeHistory);
            // null情况下，showList = historyList，无需重复添加
            if (this.target == target) {
                showList.Add(tradeHistory);
            }
            isHistoryChanged = true;
        }

        private void ReadHistory()
        {
            if (Svc.ClientState.LocalPlayer == null) { return; }
            var playerName = Svc.ClientState.LocalPlayer!.Name.TextValue;
            var playerWorld = Svc.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

            historyList = [];
            historyTargetSet = [];
            string path = Path.Join(tradeRecorder.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt");

            using (StreamReader reader = new(File.Open(path, FileMode.OpenOrCreate))) {
                string tradeStr;
                while ((tradeStr = reader.ReadLine() ?? "").Length > 0) {
                    TradeHistory? trade = TradeHistory.ParseFromString(tradeStr);
                    if (trade != null) {
                        historyList.Add(trade);
                        historyTargetSet.Add(trade.Target);
                    }
                }
            }
            if (target != null) {
                showList = historyList.Where(i => i.Target == target).ToList();
            } else {
                showList = historyList;
            }
        }

        private void SaveHistory()
        {
            if (Svc.ClientState.LocalPlayer == null) { return; }
            var playerName = Svc.ClientState.LocalPlayer!.Name.TextValue;
            var playerWorld = Svc.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

            using FileStream stream = File.Open(Path.Join(tradeRecorder.PluginInterface.ConfigDirectory.FullName, $"{playerWorld}_{playerName}.txt"), FileMode.Create);
            StreamWriter writer = new(stream);
            foreach (TradeHistory tradeHistory in historyList) {
                if (tradeHistory.visible) { writer.WriteLine(tradeHistory.ToString()); }
            }
            writer.Flush();
        }

        /// <summary>
        /// 导出当前角色所有交易记录
        /// </summary>
        /// <param name="path">保存路径</param>
        private void ExportHistory(string path)
        {
            if (Svc.ClientState.LocalPlayer == null) { return; }
            Svc.PluginLog.Information($"[{tradeRecorder.Name}]保存交易历史: {path}");

            var saveList = showList.Where(i => i.visible).Select(i => new string[7] {
                    i.Time,
                    i.Status.ToString(),
                    i.Target,
                    i.GiveGil.ToString("#,0"),
                    i.ReceiveGil.ToString("#,0"),
                    string.Join(',', i.giveItemArray.Select(i => i.ToString())),
                    string.Join(',', i.receiveItemArray.Select(i => i.ToString()))
                }).ToList();
            try {
                using StreamWriter writer = new(File.Open(path, FileMode.Create), Encoding.UTF8);
                // 写入标题
                writer.WriteLine(string.Join(",", Title.Select(str => $"\"{str}\"").ToList()));

                saveList.ForEach(i => writer.WriteLine(string.Join(",", i.Select(str => $"\"{str}\"").ToList())));
                writer.Flush();
            } catch (IOException e) {
                Svc.PluginLog.Error(e.ToString());
            }

        }


        /// <summary>
        /// 删除与指定角色交易记录
        /// </summary>
        /// <param name="target">需要删除的交易对象，为空时删除全部</param>
        private void ClearHistory(string? target = null)
        {
            if (Svc.ClientState.LocalPlayer == null) {
                Svc.PluginLog.Warning("获取角色信息失败，无法获取交易记录");
                return;
            }
            var playerName = Svc.ClientState.LocalPlayer!.Name.TextValue;
            var playerWorld = Svc.ClientState.LocalPlayer!.HomeWorld.GameData!.Name.RawString;

            if (string.IsNullOrEmpty(target)) {
                historyList.Clear();
                showList.Clear();
            } else {
                historyList.RemoveAll(i => i.Target == target);
                historyTargetSet.Remove(target);
                showList = historyList.Where(i => i.Target == this.target).ToList();
            }
            isHistoryChanged = true;
        }

        #region init
        public History(Cashier tradeRecorder)
        {
            this.tradeRecorder = tradeRecorder;
            Task.Run(() => ReadHistory());
        }
        public void Dispose()
        {
            if (isHistoryChanged) { SaveHistory(); }
        }
        #endregion
    }
}
