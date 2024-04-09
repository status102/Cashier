using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Cashier.Commons
{
	public class Svc
	{
		public static void Initialize(DalamudPluginInterface pluginInterface) => pluginInterface.Create<Svc>();

		[PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
		[PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] public static ICommandManager Commands { get; private set; } = null!;
		[PluginService] public static IDataManager DataManager { get; private set; } = null!;
		[PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
		[PluginService] public static IChatGui ChatGui { get; private set; } = null!;
		[PluginService] public static IClientState ClientState { get; private set; } = null!;
		[PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
		[PluginService] public static IGameGui GameGui { get; private set; } = null!;
		[PluginService] public static IGameNetwork GameNetwork { get; private set; } = null!;
		[PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
		[PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
		[PluginService] public static IAddonEventManager AddonEventManager { get; private set; } = null!;
		[PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
		[PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;
	}
}
