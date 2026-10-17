using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_Sprays.Managers;

public class EventManager(CS2_Sprays plugin)
{
    private readonly CS2_Sprays _plugin = plugin;

    public void RegisterEvents()
    {
        _plugin.RegisterListener<Listeners.OnServerPrecacheResources>((ResourceManifest manifest) =>
        {
            manifest.AddResource(SprayManager.SoundEventsFile);
            foreach (var spray in _plugin.Config.Sprays)
            {
                manifest.AddResource(spray.Material);
            }
        });

        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        _plugin.RegisterEventHandler<EventRoundPoststart>(OnRoundPoststart);
        _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    }

    private void OnMapStart(string mapName)
    {
        // The panel and the sprays are entities of the previous map
        _plugin.SprayPanel!.Destroy();
        _plugin.SprayManager!.OnMapStart();
    }

    private HookResult OnRoundPoststart(EventRoundPoststart @event, GameEventInfo info)
    {
        // The round restart removed the panel entity too
        _plugin.SprayPanel!.Destroy();
        _plugin.SprayManager!.OnRoundPoststart();
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (!_plugin.Config.ResetCooldownOnKill)
            return HookResult.Continue;

        var attacker = @event.Attacker;
        if (attacker is { IsValid: true, IsBot: false } && attacker != @event.Userid)
            _plugin.SprayManager!.ResetCooldown(attacker);

        return HookResult.Continue;
    }

    private void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
    {
        if (!_plugin.Config.SprayWithUseKey || (pressed & PlayerButtons.Use) == 0)
            return;

        if (!player.IsValid || player.IsBot || !_plugin.SprayManager!.HasPermission(player))
            return;

        _plugin.SprayManager.TrySpray(player, quiet: true);
    }
}
