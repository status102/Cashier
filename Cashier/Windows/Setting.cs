using Cashier.Commons;
using Cashier.Model;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Cashier.Windows
{
    public sealed class Setting : IWindow
    {
        private readonly static Vector2 Window_Size = new(720, 640);
        private readonly static Vector2 Image_Size = new(54, 54);
        private const int Item_Width = 190, Item_Internal = 5;

        private readonly Cashier _cashier;
        private Configuration Config => _cashier.Config;

        private bool _visible = false;

        private string _editName = "", _editSetPrice = "", _editSetCount = "", _editStackPrice = "";
        private bool _editQuality = false;

        private Preset? _editItem = null;
        private List<Preset> _presetList => Config.PresetList;

        private static IDalamudTextureWrap? FailureImage => PluginUI.GetIcon(784);
        public Setting(Cashier cashier)
        {
            _cashier = cashier;
        }

        public void Show()
        {
            _visible = !_visible;
        }

        public void Draw()
        {
            if (!_visible) {
                _editItem = null;
                return;
            }
            ImGui.SetNextWindowSize(Window_Size, ImGuiCond.Once);
            if (ImGui.Begin($"设置##{Cashier.Name}Config", ref _visible)) {
                if (ImGui.CollapsingHeader("基础设置", ImGuiTreeNodeFlags.DefaultOpen)) {
                    ImGui.Indent();

                    ImGui.Checkbox("显示交易窗口", ref Config.ShowTradeWindow);
                    ImGui.Checkbox("交易结束后在聊天框发送提示及结算", ref Config.TradeNotify);

                    ImGui.Unindent();
                }

#if DEBUG
                if (ImGui.CollapsingHeader("预期价格")) {

                    #region 按钮块
                    //添加预期的按钮
                    if (Utils.DrawIconButton(FontAwesomeIcon.Plus, -1)) {
                        Preset item = new(string.Empty, false);
                        _presetList.Add(item);
                        EditItem(item);

                        string clipboard = "";
                        _editQuality = false;
                        try {
                            clipboard = ImGui.GetClipboardText().Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                        } catch (NullReferenceException) { }
                        if (clipboard.Contains(SeIconChar.HighQuality.ToIconString())) {
                            _editQuality = true;
                            clipboard = clipboard.Replace(SeIconChar.HighQuality.ToIconString(), string.Empty);
                        }
                        _editName = "";
                        _editSetPrice = "0";
                        _editSetCount = "1";
                        _editStackPrice = "0";
                    }

                    //删除所有预期
                    ImGui.SameLine();
                    if (Utils.DrawIconButton(FontAwesomeIcon.Trash, -1)) {
                        _presetList.Clear();
                        Config.Save();
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("删除所有项目");
                    }

                    //导出到剪贴板
                    ImGui.SameLine();
                    if (Utils.DrawIconButton(FontAwesomeIcon.Upload, -1)) {
                        ImGui.SetClipboardText(JsonConvert.SerializeObject(_presetList));
                        Chat.PrintLog($"导出{_presetList.Count}个预设至剪贴板");
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("导出到剪贴板");
                    }

                    //从剪贴板导入
                    ImGui.SameLine();
                    if (Utils.DrawIconButton(FontAwesomeIcon.Download, -1)) {
                        try {
                            string clipboard = ImGui.GetClipboardText().Trim().Replace("/r", string.Empty).Replace("/n", string.Empty);
                            var items = JsonConvert.DeserializeObject<List<Preset>>(clipboard) ?? new();
                            foreach (var item in items) {
                                var exist = Config.PresetList.FindIndex(i => i.Name == item.Name && i.Quality == item.Quality);
                                if (exist == -1) {
                                    Config.PresetList.Add(item);
                                } else {
                                    Config.PresetList[exist] = item;
                                }
                            }
                            Chat.PrintWarning($"从剪贴板导入{items.Count}个预设");
                        } catch (Exception e) {
                            Chat.PrintMsg("从剪贴板导入失败");
                            Svc.Log.Error("从剪贴板导入失败" + e.ToString());
                        }
                        Config.Save();
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("从剪贴板导入");
                    }

                    ImGui.SameLine();
                    ImGui.TextDisabled("(?)");
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("双击鼠标左键物品编辑物品名，单击鼠标右键打开详细编辑\n不同价格方案间以;分割\n当最低价低于设定价格时，标黄进行提醒");
                    }
                    #endregion

                    //添加or编辑预期中
                    DrawEditBlock();

                    int rowIndex = 0;
                    for (int i = 0; i < _presetList.Count; i++) {
                        if ((ImGui.GetColumnWidth() + 15) < (Item_Width + Item_Internal) * (rowIndex + 1) + 8) {
                            rowIndex = 0;
                        }

                        if (rowIndex > 0) {
                            ImGui.SameLine(rowIndex * (Item_Width + Item_Internal) + 8);
                        }

                        rowIndex++;

                        DrawItemBlock(i, _presetList[i]);
                    }
                }
#endif
                ImGui.End();
            }
        }
        private void EditItem(Preset item)
        {
            _editItem = item;
            _editName = _editItem.Name;
            _editQuality = _editItem.Quality;
            _editSetPrice = _editItem.SetPrice.ToString();
            _editSetCount = _editItem.SetCount.ToString();
            _editStackPrice = _editItem.StackPrice.ToString();
        }
        private void DrawEditBlock()
        {
            if (_editItem == null)
                return;
            bool save = false;

            //保存设置
            ImGui.SameLine();
            if (Utils.DrawIconButton(FontAwesomeIcon.Check, -1) && !string.IsNullOrEmpty(_editName)) {
                save = true;
            }

            //取消编辑
            ImGui.SameLine();
            if (Utils.DrawIconButton(FontAwesomeIcon.Times, -1)) {
                _editItem = null;
                return;
            }

            // 光标+回车 自动保存
            ImGui.InputText("名字", ref _editName, 1288, ImGuiInputTextFlags.CharsNoBlank);
            if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) {
                save = true;
            }
            ImGui.SameLine();
            ImGui.Checkbox(SeIconChar.HighQuality.ToIconString(), ref _editQuality);

            ImGui.SetNextItemWidth(80);
            ImGui.InputText(SeIconChar.Gil.ToIconString() + "每", ref _editSetPrice, 32, ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) {
                save = true;
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(40);
            ImGui.InputText("个", ref _editSetCount, 4, ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) {
                save = true;
            }
            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();
            ImGui.TextUnformatted("每组");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputText(SeIconChar.Gil.ToIconString(), ref _editStackPrice, 32, ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemFocused() && ImGui.GetIO().KeysDown[13]) {
                save = true;
            }

            // todo 候选表尝试使用弹出菜单
            int current_index = -1;
            string[] items = SearchName(_editName).ToArray();
            if (ImGui.ListBox("##候选表", ref current_index, items, items.Length, 3)) {
                _editName = items[current_index];
            }

            if (save) {
                var sameIndex = _presetList.FindIndex(i => i.Name == _editName && i.Quality == _editQuality);
                if (sameIndex != -1 && _presetList.IndexOf(_editItem) != sameIndex) {
                    if (_editItem.Id == 0) {
                        _presetList.Remove(_editItem);
                    }
                    Chat.PrintWarning("物品与已有设定重复，无法添加");
                    save = false;
                    _editItem = null;
                } else {
                    if (_editName == string.Empty) {
                        _presetList.Remove(_editItem);
                    } else {
                        uint setPrice = uint.Parse(0 + _editSetPrice);
                        uint setCount = uint.Parse(0 + _editSetCount);
                        uint stackPrice = uint.Parse(0 + _editStackPrice);

                        _presetList[_presetList.IndexOf(_editItem)] = new(_editName, _editQuality, setPrice, setCount, stackPrice);
                    }

                    Config.Save();
                    save = false;
                    _editItem = null;
                }
            }
        }
        /// <summary>
        /// 绘制单个道具的方块
        /// </summary>
        /// <param name="id">通过不重复的id来区别不同东西</param>
        /// <param name="item"></param>
        private unsafe void DrawItemBlock(int index, Preset item)
        {
            if (item.Id == 0) {
                return;
            }
            ImGui.PushID(index);

            if (ImGui.BeginChild($"##ItemBlock-{index}", new(Item_Width, Image_Size.Y + 16), true)) {
                // 左侧物品图标
                IDalamudTextureWrap? texture = PluginUI.GetIcon(item.IconId, item.Quality);
                if (texture != null) {
                    ImGui.Image(texture.ImGuiHandle, Image_Size);
                } else if (FailureImage != null) {
                    ImGui.Image(FailureImage.ImGuiHandle, Image_Size);
                }

                ImGui.SameLine();
                ImGui.BeginGroup();

                ImGui.TextUnformatted(item.Name + (item.Quality ? SeIconChar.HighQuality.ToIconString() : string.Empty));

                if (item.Marketable) {
                    ImGui.TextUnformatted(item.GetPresetString());
                } else {
                    // 如果不能在市场出售
                    ImGui.TextDisabled(item.GetPresetString());
                }
                ImGui.EndGroup();
                ImGui.EndChild();
            }
            if (ImGui.IsItemHovered()) {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                    ImGui.OpenPopup($"MoreEdit-{index}");
                }
            }
            if (ImGui.BeginPopup($"MoreEdit-{index}", ImGuiWindowFlags.NoMove)) {

                // 编辑当前物品
                if (Utils.DrawIconButton(FontAwesomeIcon.Edit)) {
                    EditItem(item);
                    ImGui.CloseCurrentPopup();
                }

                // 删除当前物品
                ImGui.SameLine();
                if (Utils.DrawIconButton(FontAwesomeIcon.Trash)) {
                    _presetList.Remove(item);
                    Config.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        /// <summary>
        /// 搜索含有关键词的道具名称
        /// </summary>
        private static List<string> SearchName(string name, bool onlyTradable = true)
        {
            var resultList = new List<string>();
            if (!string.IsNullOrEmpty(name)) {
                List<string>? tradeList;
                if (onlyTradable) {
                    tradeList = Svc.DataManager.GetExcelSheet<Item>()?.Where(i => i.Name.ToString().Contains(name) && !i.IsUntradable).OrderByDescending(i => i.RowId).Select(i => i.Name.RawString).ToList();
                } else {
                    tradeList = Svc.DataManager.GetExcelSheet<Item>()?.Where(i => i.Name.ToString().Contains(name)).OrderByDescending(i => i.RowId).Select(i => i.Name.RawString).ToList();
                }
                tradeList?.Where(i => i.StartsWith(name)).ToList().ForEach(i => resultList.Add(i));
                tradeList?.Where(i => !i.StartsWith(name)).ToList().ForEach(i => resultList.Add(i));

            }
            return resultList;
        }
        public void Dispose() { }

    }
}
