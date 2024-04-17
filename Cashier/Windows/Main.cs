using Cashier.Addons;
using Cashier.Commons;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Timers;

namespace Cashier.Windows;
public unsafe sealed class Main : IWindow
{
    private readonly Cashier _cashier;
    private Trade Trade => _cashier.PluginUi.Trade;
    private const uint MaximumGilPerTimes = 10000;// 1_000_000;
    private readonly static Vector2 Window_Size = new(720, 640);
    private TaskManager TaskManager => Cashier.TaskManager!;
    private readonly int[] MoneyButton = [-50, -10, 10, 50];
    private readonly int[] AllMoneyButton = [-500, -100, -50, -10, 10, 50, 100, 500];


    private bool _visible = true;
    private readonly TeamHelper _teamHelper = new();
    private Dictionary<uint, int> _editPlan = [];
    private Dictionary<uint, int> _tradePlan = [];
    private Timer _selectPlayerTimer = new(2000) { AutoReset = true };
    private bool _isRunning = false;
    private int _change;
    public Main(Cashier cashier)
    {
        _cashier = cashier;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", OnTrade);
        _selectPlayerTimer.Elapsed += AutoRequestTradeTick;
    }

    public void Dispose()
    {
        _selectPlayerTimer.Dispose();
    }

    public void Show()
    {
        _visible = !_visible;
        if (_visible) {
            _teamHelper.UpdateTeamList();
        }
    }

    public void Draw()
    {
        if (!_visible) {
            return;
        }
        ImGui.SetNextWindowSize(Window_Size, ImGuiCond.Once);
        if (ImGui.Begin($"主窗口##{Cashier.Name}Main", ref _visible)) {
            ImGui.Text("老板队自动结账：");
            ImGui.Text("关闭窗口后自动中止");


            if (ImGui.Button("更新小队列表")) {
                _teamHelper.UpdateTeamList();
            }
            ImGui.SameLine();
            if (!_isRunning && ImGui.Button("开始")) {
                Start();
            } else if (_isRunning && ImGui.Button("停止")) {
                Stop();
            }

            ImGui.Separator();
            if (_isRunning) {
                ImGui.BeginDisabled();
            }

            _change = 0;
            AllMoneyButton.ToList().ForEach(num =>
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(15);
                string name = num < 0 ? num.ToString() : ("+" + num);
                if (ImGui.Button($"{name}##全体都有{name}")) {
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
            if (ImGui.Button($"归0##全体都有0")) {
                _editPlan.Clear();
            }

            _teamHelper.TeamList.ForEach(p =>
            {
#if !DEBUG
                if(p.ObjectId == Svc.ClientState.LocalPlayer!.ObjectId) {
                    return;
                }
#endif
                DrawPersonalSetting(p);
            });
            if (_isRunning) {
                ImGui.EndDisabled();
            }

            ImGui.End();
        }
    }

    private void DrawPersonalSetting(TeamHelper.TeamMember p)
    {
        ImGui.PushID(p.ObjectId.ToString());

        ImGui.Text(p.FirstName + "@" + p.World);
        ImGui.SameLine();
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
            if (ImGui.Button($"{name}##{p.ObjectId}{name}")) {
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
        ImGui.PopID();
    }

    private void Start()
    {
        _isRunning = true;
        _tradePlan = new(_editPlan.Where(i => i.Value > 0));
        _selectPlayerTimer.Start();
    }

    private void Stop()
    {
        _isRunning = false;
        _selectPlayerTimer.Stop();
    }

    private void AutoRequestTradeTick(object? sender, ElapsedEventArgs e)
    {
        if (!_visible || _tradePlan.Count == 0) {
            Stop();
            return;
        }
        if (Trade.IsTrading) {
            return;
        }
        _tradePlan.Keys
             .Where(id => AddonTradeHelper.IsDistanceEnough(Svc.ObjectTable.SearchById(id)?.Position ?? new()))
            .ToList()
            .ForEach(id =>
            {
                TaskManager.Enqueue(() => AddonTradeHelper.RequestTrade(id));
            });
    }

    private void OnTrade(AddonEvent type, AddonArgs args)
    {
        if (!_visible) {
            return;
        }
        if (_tradePlan.TryGetValue(Trade.Target.ObjectId, out var value)) {
            Commons.Chat.PrintLog($"auto: to{Trade.Target.PlayerName}={value}");
            TaskManager.Enqueue(() => AddonTradeHelper.SetGil(value >= MaximumGilPerTimes ? MaximumGilPerTimes : (uint)value));
            TaskManager.DelayNext(50);
            TaskManager.Enqueue(AddonTradeHelper.Step.PreCheck);
        } else {
            AddonTradeHelper.Cancel();
        }

    }

    public void OnTradeFinalChecked(uint objectId, uint money)
    {
        if (!_visible) {
            return;
        }

        if (!_tradePlan.TryGetValue(objectId, out var value)) {
        } else if (money <= value) {
            TaskManager.DelayNext(50);
            TaskManager.Enqueue(() => AddonTradeHelper.Step.FinalCheck());
        }
    }

    public void OnTradeFinished(uint objectId, uint money)
    {
        if (!_visible) {
            return;
        }
        if (!_tradePlan.ContainsKey(objectId)) {

        } else {
            _tradePlan[objectId] -= (int)money;
            if (_tradePlan[objectId] <= 0) {
                _tradePlan.Remove(objectId);
            }
        }
        if (_tradePlan.Count == 0) {
            Stop();
        }
    }
}
