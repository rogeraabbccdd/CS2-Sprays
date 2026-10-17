using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace CS2_Sprays.Managers;

public class CommandsManager(CS2_Sprays plugin)
{
    private readonly CS2_Sprays _plugin = plugin;

    public void RegisterCommands()
    {
        _plugin.AddCommand("css_sprays", "Choose a spray", OnSprays);
        _plugin.AddCommand("css_spray", "Use the selected spray", OnSpray);
    }

    private bool CanUse(CCSPlayerController player)
    {
        if (_plugin.SprayManager!.HasPermission(player))
            return true;

        player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoAccess"]}");
        return false;
    }

    private void OnSprays(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || !CanUse(player)) return;

        if (_plugin.Config.Sprays.Count == 0)
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoSprays"]}");
            return;
        }

        if (!_plugin.SprayPanel!.Open(player))
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["MenuFailed"]}");
        }
    }

    private void OnSpray(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || !CanUse(player)) return;

        _plugin.SprayManager!.TrySpray(player);
    }
}
