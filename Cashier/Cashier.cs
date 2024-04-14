using Cashier.Addons;
using Cashier.Commons;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Cashier
{
    public unsafe sealed class Cashier : IDalamudPlugin
    {
        public static TaskManager? TaskManager { get; private set; }
        public static string PluginName { get; } = "Cashier";
        public string Name => "Cashier";
        private const string commandName = "/ca";
        public static Cashier? Instance { get; private set; }
        public PluginUI PluginUi { get; init; }
        public Configuration Config { get; init; }
        public DalamudPluginInterface PluginInterface { get; init; }
        public uint homeWorldId = 0;
        private HookHelper HookHelper { get; init; }

        public Cashier(DalamudPluginInterface pluginInterface)
        {
            Instance = this;
            PluginInterface = pluginInterface;

            Svc.Initialize(pluginInterface);
            Config = PluginInterface.GetPluginConfig() as Configuration ?? new();
            Config.Initialize(PluginInterface);

            Svc.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "/ca 打开历史记录\n /ca config|cfg 打开设置窗口"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Svc.ClientState.Login += OnLogin;
            Svc.ClientState.Logout += OnLogout;

            PluginUi = new PluginUI(this, Config);
            homeWorldId = Svc.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;

            //Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Trade", PluginUi.Trade.TradeUpdate);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", PluginUi.Trade.TradeShow);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Trade", PluginUi.Trade.TradeHide);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "Trade", (events, args) =>
            {
                //Svc.PluginLog.Debug("PostUpdate");
            });
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Trade", PluginUi.Trade.AddonTradeReceiveEvent);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Trade", (events, args) =>
            {
                Svc.PluginLog.Debug("PostRefresh");
            });


            ECommonsMain.Init(pluginInterface, this);
            TaskManager = new();
            HookHelper = new(this);

            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon)) {
                //Svc.ChatGui.Print("没找到注入");
                // 无用
            } else {
            }
        }

        public void Dispose()
        {
            HookHelper.Dispose();
            TaskManager!.Abort();
            ECommonsMain.Dispose();

            Svc.ClientState.Login -= OnLogin;
            Svc.ClientState.Logout -= OnLogout;
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            Svc.CommandManager.RemoveHandler(commandName);
            //Svc.AddonLifecycle.UnregisterListener([PluginUi.Trade.TradeShow, PluginUi.Trade.TradeHide]);
            PluginUi.Dispose();
        }

        private unsafe void OnCommand(string command, string args)
        {
            string arg = args.Trim().Replace("\"", string.Empty);
            if (string.IsNullOrEmpty(arg)) {
                PluginUi.History.ShowHistory();
            } else if (arg == "cfg" || arg == "config") {
                PluginUi.Setting.Show();
            } else if (arg == "t") {
                var id = TargetSystem.Instance()->Target;
                Commons.Chat.PrintLog($"ID:{id->ObjectID:X}||{(nint)id:X}");
            } else if (arg == "tt") {
            } else if (arg.StartsWith("money")) {
                if (!int.TryParse(arg[5..].Trim(), out int result)) {
                    Commons.Chat.PrintLog("money parse error" + arg);
                    return;
                }
                AddonTradeHelper.TradeGil(result);
            } else if (arg == "cancel") {
                AddonTradeHelper.CancelTrade();
            } else if (arg.StartsWith("trade name")) {
                AddonTradeHelper.RequestTrade(arg[10..].Trim());
            } else if (arg.StartsWith("confirm")) {
                AddonTradeHelper.ConfirmTradeFirst();
            }
#if DEBUG
            else if (arg == "test") {
                Commons.Chat.PrintLog("服务器id:" + homeWorldId);
            }
#endif
        }

        private void DrawUI()
        {
            // todo 有Bug system.nullReference
            PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.Setting.Show();
        }

        private void OnLogin()
        {
            homeWorldId = Svc.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;
        }

        private void OnLogout()
        {
            homeWorldId = 0;
        }
    }
}
