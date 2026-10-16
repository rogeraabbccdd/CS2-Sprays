using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using Clientprefs.API;
using CS2_Sprays.Config;
using CS2_Sprays.Managers;
using CS2_Sprays.Menu;
using Microsoft.Extensions.Logging;

namespace CS2_Sprays;

// 374 added the custom_hud_layout API (panorama menu) and Trace
[MinimumApiVersion(374)]
public class CS2_Sprays : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "CS2-Sprays";

    public override string ModuleVersion => "1.0.0";

    // Based on CS2-Poor-MapAdvertisements by Letaryat
    public override string ModuleAuthor => "Kento, Letaryat";

    public override string ModuleDescription => "Player sprays with a panorama picker.";

    public required PluginConfig Config { get; set; }

    public static CS2_Sprays? Instance { get; private set; }

    public EventManager? EventManager { get; private set; }
    public SprayManager? SprayManager { get; private set; }
    public CommandsManager? CommandsManager { get; private set; }
    public SprayPanel? SprayPanel { get; private set; }

    private readonly PluginCapability<IClientprefsApi> _clientprefsCapability = new("Clientprefs");

    public override void Load(bool hotReload)
    {
        Instance = this;

        EventManager = new EventManager(this);
        SprayManager = new SprayManager(this);
        CommandsManager = new CommandsManager(this);
        SprayPanel = new SprayPanel(this);

        EventManager.RegisterEvents();
        CommandsManager.RegisterCommands();
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IClientprefsApi? clientprefs = null;
        try
        {
            clientprefs = _clientprefsCapability.Get();
        }
        catch (Exception ex)
        {
            DebugMode($"Clientprefs capability: {ex}");
        }

        SprayManager!.LoadClientprefs(clientprefs, hotReload);
    }

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
    }

    public override void Unload(bool hotReload)
    {
        SprayManager?.UnloadClientprefs();
        SprayPanel?.Destroy();
    }

    public void DebugMode(string message)
    {
        if (Config.Debug)
        {
            Logger.LogInformation(message);
        }
    }
}
