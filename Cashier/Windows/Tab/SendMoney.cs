using Cashier.Addons;
using Cashier.Commons;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace Cashier.Windows.Tab;
public sealed class SendMoney : ITabPage
{
    public string TabName { get; } = "老板队自动结账";
    private readonly Cashier _cashier;
    private Trade Trade => _cashier.PluginUi.Trade;
    private Configuration Config => _cashier.Config;
    private static TaskManager TaskManager => Cashier.TaskManager!;
    private int RandomDelay => Random.Next(50, 200);
    private readonly Random Random = new(DateTime.Now.Millisecond);
    private const uint Maximum_Gil_Per_Times = 1_000_000;
    private int[] MoneyButton = [-50, -10, 10, 50];

    private Dictionary<uint, int> _editPlan = [];
    private Dictionary<uint, int> _tradePlan = [];
    private Dictionary<uint, int> _tradePriority = [];
    private bool _enhance = true;
    private readonly TeamHelper _teamHelper = new();
    private readonly Timer _selectPlayerTimer = new(1_000) { AutoReset = true };
    private bool _isRunning = false;
    private double _allMoney;
    private int _change;
    private uint _lastTradeObjectId;
    private float _nameLength;

    public SendMoney(Cashier cashier)
    {
        _cashier = cashier;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", OnTrade);
        _selectPlayerTimer.Elapsed += AutoRequestTradeTick;
    }

    public void Show()
    {
        UpdateTeamList();
    }

    public void Dispose()
    {
        _selectPlayerTimer.Dispose();
    }

    public void Draw()
    {
        if (ImGui.Button("更新小队列表")) {
            UpdateTeamList();
        }
        ImGui.SameLine();
        if (!_isRunning && ImGui.Button("开始")) {
            Start();
        } else if (_isRunning && ImGui.Button("停止")) {
            Stop();
        }
        ImGui.SameLine();
        ImGui.Checkbox("增强模式", ref _enhance);

        ImGui.Separator();
        if (_isRunning) {
            ImGui.BeginDisabled();
        }

        _change = 0;
        ImGui.Text("全体: ");
        ImGui.SameLine(_nameLength + 60);
        ImGui.SetNextItemWidth(80);
        ImGui.InputDouble($"w##AllMoney", ref _allMoney, 0, 0, "%.1f", ImGuiInputTextFlags.CharsDecimal);
        if (ImGui.IsItemDeactivatedAfterEdit()) {
            _editPlan.Keys.ToList().ForEach(key => _editPlan[key] = (int)(_allMoney * 10000));
        }
        MoneyButton.ToList().ForEach(num =>
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(15);
            string name = num < 0 ? num.ToString() : ("+" + num);
            if (ImGui.Button($"{name}w##All{name}")) {
                _change = num * 10000;
            }
        });
        if (_change != 0) {
            _teamHelper.TeamList.ForEach(p =>
            {
                if (!_editPlan.ContainsKey(p.ObjectId)) {
                    _editPlan.Add(p.ObjectId, _change);
                } else {
                    _editPlan[p.ObjectId] += _change;
                }
            });
        }
        ImGui.SameLine();
        if (ImGui.Button($"归0##All0")) {
            foreach (var key in _editPlan.Keys) {
                _editPlan[key] = 0;
            };
        }

        _teamHelper.TeamList.ForEach(p =>
        {
            if (p.ObjectId == Svc.ClientState.LocalPlayer!.ObjectId) {
                return;
            }
            DrawPersonalSetting(p);
        });
        if (_isRunning) {
            ImGui.EndDisabled();
        }
    }

    private void DrawPersonalSetting(TeamHelper.TeamMember p)
    {
        ImGui.PushID(p.ObjectId.ToString());
        var hasPlan = _editPlan.ContainsKey(p.ObjectId);
        var text = p.FirstName + "@" + p.World;
        _nameLength = Math.Max(ImGui.CalcTextSize(text).X, _nameLength);
        if (!ImGui.Checkbox(text, ref hasPlan)) {
        } else if (hasPlan) {
            _editPlan.Add(p.ObjectId, (int)(_allMoney * 10000));
        } else {
            _editPlan.Remove(p.ObjectId);
        }
        if (hasPlan) {
            ImGui.SameLine(_nameLength + 60);
            ImGui.SetNextItemWidth(80);
            double value = _editPlan.TryGetValue(p.ObjectId, out int valueToken) ? valueToken / 10000.0 : 0;
            ImGui.InputDouble($"w##{p.ObjectId}Money", ref value, 0, 0, "%.1f", ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemDeactivatedAfterEdit()) {
                _editPlan[p.ObjectId] = (int)(value * 10000);
            }
            _change = 0;
            MoneyButton.ToList().ForEach(num =>
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(15);
                string name = num < 0 ? num.ToString() : ("+" + num);
                if (ImGui.Button($"{name}w##{p.ObjectId}{name}")) {
                    _change = num * 10000;
                }
            });
            if (_change != 0) {
                _editPlan[p.ObjectId] = (int)(value * 10000) + _change;
            }

            ImGui.SameLine();
            if (ImGui.Button($"归0##{p.ObjectId}0")) {
                _editPlan[p.ObjectId] = 0;
            }

            if (_isRunning) {
                // 运行中
                ImGui.SameLine();
                _change = _tradePlan.TryGetValue(p.ObjectId, out var valueToken2) ? valueToken2 : 0;
                ImGui.Text($"剩余: {_change:#,0}");
            }
        }
        ImGui.PopID();
    }

    private void Start()
    {
        _isRunning = true;
        _tradePlan = new(_editPlan.Where(i => i.Value > 0));
        //_tradePriority = new(_editPlan.Keys.Select(i => KeyValuePair.Create(i, 0)));
        _selectPlayerTimer.Start();
        AutoRequestTradeTick(null, null);
    }

    private void Stop()
    {
        _isRunning = false;
        _selectPlayerTimer.Stop();
        _tradePlan.Clear();
        _lastTradeObjectId = default;
    }

    private void UpdateTeamList()
    {
        _teamHelper.UpdateTeamList();
        _teamHelper.TeamList.ForEach(p =>
        {
            _editPlan.TryAdd(p.ObjectId, 0);
        });
        _nameLength = (int)ImGui.CalcTextSize("全体: ").X + 60;
        MoneyButton = [-Config.TradeStepping_2, -Config.TradeStepping_1, Config.TradeStepping_1, Config.TradeStepping_2];
    }

    /// <summary>
    /// 显示交易窗口后设置金额
    /// </summary>
    private void OnTrade(AddonEvent type, AddonArgs args)
    {
        if (!_isRunning) {
            return;
        }
        TaskManager.Abort();
        _lastTradeObjectId = Trade.Target.ObjectId;
        if (_tradePlan.TryGetValue(_lastTradeObjectId, out var value)) {
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(() => SetGil(value >= Maximum_Gil_Per_Times ? Maximum_Gil_Per_Times : (uint)value));
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(AddonTradeHelper.Step.PreCheck);
        } else {
            AddonTradeHelper.Cancel();
        }

    }

    public void OnTradePreCheckChanged(uint objectId, bool confirm, uint money)
    {
        if (!_isRunning || !confirm) {
            return;
        }
        if (!_tradePlan.TryGetValue(objectId, out var value)) {
            AddonTradeHelper.Cancel();
        } else if (money <= value) {
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(AddonTradeHelper.Step.PreCheck);
        }
    }

    public void OnTradeFinalChecked(uint objectId, uint money)
    {
        if (!_isRunning) {
            return;
        }

        if (!_tradePlan.TryGetValue(objectId, out var value)) {
        } else if (money <= value) {
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(() => AddonTradeHelper.Step.FinalCheck());
        }
    }

    public void OnTradeFinished(uint objectId, uint money)
    {
        if (!_isRunning) {
            return;
        }
        if (!_tradePlan.ContainsKey(objectId)) {
            Commons.Chat.PrintError("交易完成，但是找不到对应的交易计划");
        } else {
            _tradePlan[objectId] -= (int)money;
            if (_tradePlan[objectId] <= 0) {
                _tradePlan.Remove(objectId);
                //_tradePriority.Remove(objectId);
                _editPlan.Remove(objectId);
            }
        }
        if (_tradePlan.Count == 0) {
            Stop();
        }
    }

    private unsafe void RequestTrade(uint objectId)
    {
        var target = Svc.ObjectTable.SearchById(objectId);
        if (_enhance && target is not null) {
            TargetSystem.Instance()->Target = (GameObject*)target.Address;
            _cashier.HookHelper.DetourTradeRequest((nint)InventoryManager.Instance(), (nint)objectId);
        } else {
            AddonTradeHelper.RequestTrade(objectId);
        }
    }

    private unsafe void SetGil(uint money)
    {
        if (_enhance) {
            _cashier.HookHelper.DetourTradeMyMoney((nint)InventoryManager.Instance(), money);
        } else {
            AddonTradeHelper.Step.SetCount(money);
        }
    }
    /// <summary>
    /// 遍历交易计划发起交易申请Tick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AutoRequestTradeTick(object? sender, ElapsedEventArgs? e)
    {
        if (!_isRunning || _tradePlan.Count == 0) {
            Stop();
            return;
        }
        if (Trade.IsTrading) {
            return;
        }
        if (_lastTradeObjectId != default && _tradePlan.ContainsKey(_lastTradeObjectId)) {
            TaskManager.Enqueue(() => RequestTrade(_lastTradeObjectId));
        }
        _tradePlan.Keys.ToList()
            .ForEach(id =>
            {
                TaskManager.DelayNext(RandomDelay);
                TaskManager.Enqueue(() => RequestTrade(id));
            });
    }
}
