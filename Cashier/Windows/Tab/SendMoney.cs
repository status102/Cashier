﻿using Cashier.Addons;
using Cashier.Commons;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Cashier.Windows.Tab;
public sealed class SendMoney : TabConfigBase, ITabPage
{
    public string TabName { get; } = "自动打钱";
    public bool Hide { get; }
    private readonly Cashier _cashier;
    private Trade Trade => _cashier.Trade;
    private readonly new Configuration Config;
    private static TaskManager TaskManager => Cashier.TaskManager!;
    private int RandomDelay => Random.Next(200, 500);
    private readonly Random Random = new(DateTime.Now.Millisecond);
    private const uint Maximum_Gil_Per_Times = 1_000_000;
    private readonly TeamHelper _teamHelper = new();
    private readonly Timer _selectPlayerTimer = new(1_000) { AutoReset = true };

    private int[] MoneyButton = [-50, -10, 10, 50];
    private float _nameLength;
    private bool _enhance = true;

    private Dictionary<uint, long> _editPlan = [];
    private Dictionary<uint, long> _tradePlan = [];
    private List<Member> _memberList = [];
    private bool[] _preCheckStatus = [];

    private bool _isRunning;
    private uint _lastTradeObjectId;

    private double _planAll;
    private long _change;

    public SendMoney(Cashier cashier)
    {
        _cashier = cashier;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", OnTrade);
        _selectPlayerTimer.Elapsed += AutoRequestTradeTick;
        Config = TabConfig.LoadConfig<Configuration>(_cashier.PluginInterface.ConfigDirectory.FullName, "SendMoney");
        Config.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Config.TradeStepping_1) || e.PropertyName == nameof(Config.TradeStepping_2)) {
                MoneyButton = [-Config.TradeStepping_2, -Config.TradeStepping_1, Config.TradeStepping_1, Config.TradeStepping_2];
            }
        };
    }

    public void Dispose()
    {
        _selectPlayerTimer.Dispose();
    }

    #region UI
    public void Show()
    {
        UpdateTeamList();
    }

    public void Draw()
    {
        DrawSetting();
        ImGui.Separator();
        DrawOverallSetting();
        foreach (var p in _memberList) {
            DrawPersonalSetting(p);
        }
    }

    private void DrawSetting()
    {
        if (!_isRunning && ImGui.Button("开始")) {
            Start();
        } else if (_isRunning && ImGui.Button("停止")) {
            Stop();
        }
        ImGui.SameLine();
        if (ImGui.Button("更新小队列表")) {
            UpdateTeamList();
        }
        ImGui.SameLine();
        if (ImGui.Button("添加目标对象")) {
            AddTargetPlayer();
        }
        ImGui.SameLine();
        ImGui.Checkbox("交互增强", ref _enhance);

        ImGui.SameLine();
        int value = Config.TradeStepping_1;
        ImGui.SetNextItemWidth(50 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("步进1", ref value, 0, 0, ImGuiInputTextFlags.CharsDecimal)) {
            Config.TradeStepping_1 = value;
        }

        ImGui.SameLine();
        value = Config.TradeStepping_2;
        ImGui.SetNextItemWidth(50 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("步进2", ref value, 0, 0, ImGuiInputTextFlags.CharsDecimal)) {
            Config.TradeStepping_2 = value;
        }
    }

    private void DrawOverallSetting()
    {
        ImGui.BeginGroup();
        bool hasPlan = _editPlan.Count > 0;
        if (!ImGui.Checkbox("##AllHasPlan", ref hasPlan)) {
        } else if (hasPlan) {
            foreach (var p in _memberList.Where(p => !_editPlan.ContainsKey(p.ObjectId))) {
                _editPlan.Add(p.ObjectId, (int)(_planAll * 10000));
            }
        } else {
            _editPlan.Clear();
        }
        ImGui.SameLine();
        ImGui.Text("全体: ");
        ImGui.BeginDisabled(_isRunning);
        ImGui.SameLine(_nameLength + 60);
        ImGui.SetNextItemWidth(80);
        ImGui.InputDouble($"w##AllMoney", ref _planAll, 0, 0, "%.1lf", ImGuiInputTextFlags.CharsDecimal);
        if (ImGui.IsItemDeactivatedAfterEdit()) {
            _editPlan.Keys.ToList().ForEach(key => _editPlan[key] = (long)(_planAll * 10000));
        }

        _change = 0;
        foreach (var num in MoneyButton) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(15);
            string name = num < 0 ? num.ToString() : ("+" + num);
            if (ImGui.Button($"{name}w##All{name}")) {
                _change = num * 10000;
            }
        }

        if (_change != 0) {
            _teamHelper.TeamList.ForEach(p =>
            {
                if (!_editPlan.TryAdd(p.ObjectId, _change)) {
                    _editPlan[p.ObjectId] += _change;
                }
            });
        }

        ImGui.SameLine();
        if (ImGui.Button($"归0##All0")) {
            _planAll = 0;
            foreach (var key in _editPlan.Keys) {
                _editPlan[key] = 0;
            };
        }

        ImGui.EndDisabled();
        ImGui.EndGroup();
    }

    private void DrawPersonalSetting(Member p)
    {
        ImGui.PushID(p.ObjectId.ToString());
        ImGui.BeginGroup();
        var hasPlan = _editPlan.ContainsKey(p.ObjectId);

        ImGui.BeginDisabled(_isRunning);
        if (!ImGui.Checkbox($"##{p.FullName}-CheckBox", ref hasPlan)) {
        } else if (hasPlan) {
            _editPlan.Add(p.ObjectId, (int)(_planAll * 10000));
        } else {
            _editPlan.Remove(p.ObjectId);
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.Text(p.FullName);
        ImGui.SameLine(_nameLength + 60);
        if (!hasPlan) {
        } else if (!_isRunning) {
            ImGui.SetNextItemWidth(80);
            var value = _editPlan.TryGetValue(p.ObjectId, out var valueToken) ? valueToken / 10000.0 : 0;
            ImGui.InputDouble($"w##{p.ObjectId}-Money", ref value, 0, 0, "%.1lf", ImGuiInputTextFlags.CharsDecimal);
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
        } else {
            // 运行中
            _change = _tradePlan.TryGetValue(p.ObjectId, out var valueToken2) ? valueToken2 : 0;
            ImGui.Text($"剩余: {_change:#,0}");
        }
        ImGui.EndGroup();
        ImGui.PopID();
    }
    #endregion

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
        TaskManager.Abort();
        _tradePlan.Clear();
        _lastTradeObjectId = default;
    }

    private void UpdateTeamList()
    {
        _memberList.Clear();
        _teamHelper.UpdateTeamList();
        // 从交易计划中移除不在小队中的玩家
        _editPlan.Where(p => !_teamHelper.TeamList.Any(t => t.ObjectId == p.Key)).ToList().ForEach(p => _editPlan.Remove(p.Key));
        foreach (var item in _teamHelper.TeamList.Where(p => p.ObjectId != Svc.ClientState.LocalPlayer?.ObjectId)) {
            _memberList.Add(new(item));
            _editPlan.TryAdd(item.ObjectId, 0);
        }

        // Linq.Max在list为空时报错
        _nameLength = _memberList.Select(p => ImGui.CalcTextSize(p.FullName).X).Append(ImGui.CalcTextSize("全体: ").X).Max();
    }

    private unsafe void AddTargetPlayer()
    {
        var target = TargetSystem.Instance()->GetCurrentTarget();
        if (target is not null && Svc.ObjectTable.SearchById(target->ObjectID) is Character { ObjectKind: ObjectKind.Player } player) {
            if (_memberList.Any(p => p.ObjectId == player.ObjectId)) {
                return;
            }
            _memberList.Add(new(player));
            _editPlan.TryAdd(player.ObjectId, 0);
            _nameLength = _memberList.Select(p => ImGui.CalcTextSize(p.FullName).X).Append(ImGui.CalcTextSize("全体: ").X).Max();
        }
    }

    /// <summary>
    /// 显示交易窗口后设置金额
    /// </summary>
    private void OnTrade(AddonEvent type, AddonArgs args)
    {
        if (!_isRunning) {
            return;
        }
        _selectPlayerTimer.Stop();
        TaskManager.Abort();
        _lastTradeObjectId = Trade.Target.ObjectId;
        if (!_tradePlan.TryGetValue(_lastTradeObjectId, out var value)) {
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(AddonTradeHelper.Cancel);
        } else {
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(() => SetGil(value >= Maximum_Gil_Per_Times ? Maximum_Gil_Per_Times : (uint)value));
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(ConfirmPreCheck);
        }
    }

    public void OnTradePreCheckChanged(uint objectId, bool[] confirm, uint money)
    {
        if (!_isRunning) {
            return;
        }
        _preCheckStatus = confirm;
        if (!_tradePlan.TryGetValue(objectId, out var value)) {
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(AddonTradeHelper.Cancel);
            return;
        } else if (money <= value && !confirm[0] && confirm[1]) {
            TaskManager.DelayNext(RandomDelay);
            TaskManager.Enqueue(ConfirmPreCheck);
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

    public void OnTradeCancelled()
    {
        if (!_isRunning) {
            return;
        }
        _selectPlayerTimer.Start();
    }

    public void OnTradeFinished(uint objectId, uint money)
    {
        if (!_isRunning) {
            return;
        }
        if (!_tradePlan.ContainsKey(objectId)) {
            Svc.Log.Warning("交易完成，但是找不到对应的交易计划");
        } else {
            _tradePlan[objectId] -= (int)money;
            if (_tradePlan[objectId] <= 0) {
                _tradePlan.Remove(objectId);
                _editPlan.Remove(objectId);
            }
        }
        _selectPlayerTimer.Start();
        if (_tradePlan.Count == 0) {
            Stop();
        }
    }

    private unsafe void RequestTrade(uint objectId)
    {
        var target = Svc.ObjectTable.SearchById(objectId);
        if (_enhance && target is not null) {
            TargetSystem.Instance()->Target = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)target.Address;
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

    private void ConfirmPreCheck()
    {
        if (!_preCheckStatus[0]) {
            AddonTradeHelper.Step.PreCheck();
            _preCheckStatus[0] = true;
        }
    }

    /// <summary>
    /// 遍历交易计划发起交易申请Tick
    /// </summary>
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

    [Serializable]
    class Configuration : TabConfig
    {
        private int _tradeStepping_1 = 10;

        /// <summary>
        /// 结账的步进值
        /// </summary>
        [JsonProperty("trade_stepping_1")]
        public int TradeStepping_1
        {
            get => _tradeStepping_1;
            set => SetAndNotify(ref _tradeStepping_1, value);
        }

        private int _tradeStepping_2 = 50;
        [JsonProperty("trade_stepping_2")]
        public int TradeStepping_2
        {
            get => _tradeStepping_2;
            set => SetAndNotify(ref _tradeStepping_2, value);
        }
    }

    class Member
    {
        public uint ObjectId;
        public string FirstName = null!;
        public string World = null!;

        public Member(TeamHelper.TeamMember teamMember)
        {
            ObjectId = teamMember.ObjectId;
            FirstName = teamMember.FirstName;
            World = teamMember.World;
        }

        public unsafe Member(Character gameObject)
        {
            ObjectId = gameObject.ObjectId;
            FirstName = gameObject.Name.TextValue;
            var worldId = ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)gameObject.Address)->HomeWorld;
            World = Svc.DataManager.GetExcelSheet<World>()?.FirstOrDefault(x => x.RowId == worldId)?.Name ?? "???";
        }

        public string FullName => $"{FirstName}@{World}";
    }
}