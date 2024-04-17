using Cashier.Addons;
using Cashier.Commons;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace Cashier
{
    public unsafe sealed class Cashier : IDalamudPlugin
    {
        public static TaskManager? TaskManager { get; private set; }
        public static string PluginName { get; } = "Cashier";
        public static string Name { get; } = "Cashier";
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
                HelpMessage = "/ca 打开主窗口\n/ca config|cfg 打开设置窗口"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Svc.ClientState.Login += OnLogin;
            Svc.ClientState.Logout += OnLogout;

            PluginUi = new(this);
            homeWorldId = Svc.ClientState.LocalPlayer?.HomeWorld.Id ?? homeWorldId;

            ECommonsMain.Init(pluginInterface, this);
            TaskManager = new();
            HookHelper = new(this);
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
                PluginUi.Main.Show();
            } else if (arg == "cfg" || arg == "config") {
                PluginUi.Setting.Show();
            } else if (arg == "t") {
                var id = TargetSystem.Instance()->Target;
                Commons.Chat.PrintLog($"ID:{id->ObjectID:X}||{(nint)id:X}");
            } else if (arg == "tt") {
            } else if (arg.StartsWith("money")) {
                if (!uint.TryParse(arg[5..].Trim(), out uint result)) {
                    Commons.Chat.PrintLog("money parse error" + arg);
                    return;
                }
                AddonTradeHelper.SetGil(result);
            } else if (arg == "cancel") {
                AddonTradeHelper.Cancel();
            } else if (arg.StartsWith("trade name")) {
                AddonTradeHelper.RequestTrade(arg[10..].Trim());
            }
#if DEBUG
            else if (arg == "test") {
                Commons.Chat.PrintLog("服务器id:" + homeWorldId);
            }
#endif
        }

        private void DrawUI()
        {
            PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            PluginUi.Setting.Show();
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
