using Cashier.Commons;
using Cashier.Windows;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using Chat = Cashier.Commons.Chat;

namespace Cashier;
public unsafe sealed class Cashier : IDalamudPlugin
{
    public static string Name { get; } = "Cashier";
    private const string commandName = "/ca";

    public static Cashier? Instance { get; private set; }
    public DalamudPluginInterface PluginInterface { get; init; }
    public PluginUI PluginUi { get; init; }
    public Configuration Config { get; init; }
    public static TaskManager? TaskManager { get; private set; }

    public History History { get; init; }
    public Trade Trade { get; init; }
    public Setting Setting { get; init; }
    public Main Main { get; init; }
    public HookHelper HookHelper { get; init; }

    public Cashier(DalamudPluginInterface pluginInterface)
    {
        Instance = this;
        PluginInterface = pluginInterface;

        Svc.Initialize(pluginInterface);
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new();
        Config.Initialize(PluginInterface);

        HookHelper = new(this);
        PluginUi = new(this);
        Trade = new(this);
        History = new(this);
        Setting = new(this);
        Main = new(this);

        Svc.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "/ca 打开主窗口\n/ca config|cfg 打开设置窗口\n/ca history 打开历史记录"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += UiBuilder_OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        Svc.ClientState.Login += OnLogin;
        Svc.ClientState.Logout += OnLogout;

        ECommonsMain.Init(pluginInterface, this);
        TaskManager = new();
    }

    public void Dispose()
    {
        HookHelper.Dispose();
        TaskManager!.Abort();
        ECommonsMain.Dispose();

        Svc.ClientState.Login -= OnLogin;
        Svc.ClientState.Logout -= OnLogout;
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= UiBuilder_OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Svc.CommandManager.RemoveHandler(commandName);
        PluginUi.Dispose();
        Trade.Dispose();
        Setting.Dispose();
        History.Dispose();
        Main.Dispose();
    }

    private unsafe void OnCommand(string command, string args)
    {
        string arg = args.Trim().Replace("\"", string.Empty);
        if (string.IsNullOrEmpty(arg)) {
            Main.Show();
        } else if (arg == "cfg" || arg == "config") {
            Setting.Show();
        } else if (arg == "history") {
            History.Show();
        }
#if DEBUG
        else if (arg == "t") {
            var id = TargetSystem.Instance()->Target;
            Chat.PrintLog($"ID:{id->ObjectID:X}||{(nint)id:X}");
        }
#endif
    }

    private void DrawUI()
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

    private void UiBuilder_OpenMainUi()
    {
        Main.Show();
    }
    private void DrawConfigUI()
    {
        Setting.Show();
    }

    private void OnLogin()
    {
    }

    private void OnLogout()
    {
    }
}