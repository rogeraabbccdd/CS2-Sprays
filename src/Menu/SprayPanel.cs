using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_Sprays.Menu;

/// Paged spray picker drawn on a custom_hud_layout panel from the workshop addon
/// (panorama/layout/custom_game/sprays.xml). One entity is shared by every player who has it
/// open; each player's page is written with the *ForPlayer calls, so players browse independently.
public class SprayPanel
{
    public const int PageSize = 12;

    private const string DialogPanel = "spray_dialog";
    private const string HeaderPanel = "spray_header";
    private const string PagePanel = "spray_page";
    private const string PrevPanel = "spray_prev";
    private const string NextPanel = "spray_next";
    private const string ClosePanel = "spray_close";
    private const string CardPrefix = "spray_card_";
    private const string ImagePrefix = "spray_img_";
    private const string NamePrefix = "spray_name_";

    private const string OpenClass = "spray-open";
    private const string EmptyClass = "spray-empty";
    private const string SelectedClass = "spray-selected";
    private const string ButtonOffClass = "spray-btn-off";
    private const string RandomImageClass = "spray-random";

    private readonly CS2_Sprays _plugin;
    private CCSCustomHudLayout? _hud;

    // Slot -> current page of every player who has the panel open
    private readonly Dictionary<int, int> _pages = new();

    // Picture class currently set on each card, per slot. Classes accumulate on the entity,
    // so the previous one has to be removed before the next is added.
    private readonly Dictionary<int, string?[]> _appliedImages = new();

    public SprayPanel(CS2_Sprays plugin)
    {
        _plugin = plugin;
        plugin.RegisterListener<Listeners.OnCustomHudClicked>(OnHudClicked);
        plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect, HookMode.Pre);
    }

    // Cards in menu order; null is the Random card
    private List<Models.SprayModel?> Entries => _plugin.Config.RandomSpray ? [null, .. _plugin.Config.Sprays] : [.. _plugin.Config.Sprays];
    private int PageCount(int count) => Math.Max(1, (count + PageSize - 1) / PageSize);

    private static bool IsSelected(Models.SprayModel? entry, string? selection) =>
        entry == null ? selection == Managers.SprayManager.RandomSelection : entry.Name == selection;

    // Opens the panel on the page holding the player's current spray
    public bool Open(CCSPlayerController player)
    {
        try
        {
            if (!EnsureHud(out var hud))
                return false;

            var selection = _plugin.SprayManager!.GetSelection(player);
            int index = Entries.FindIndex(e => IsSelected(e, selection));
            _pages[player.Slot] = Math.Max(0, index) / PageSize;

            Render(hud, player);
            hud.SetHasClassForPlayer(player, DialogPanel, OpenClass, true);
            hud.SetInputCaptureEnabled(player, true);
            return true;
        }
        catch (Exception ex)
        {
            _plugin.DebugMode($"[SprayPanel] Failed to open for slot {player.Slot}: {ex}");
            _pages.Remove(player.Slot);
            if (_pages.Count == 0)
                Destroy();
            return false;
        }
    }

    public void Close(CCSPlayerController player)
    {
        if (!_pages.Remove(player.Slot))
            return;

        if (_hud is { IsValid: true } hud && player.IsValid)
        {
            try
            {
                hud.SetHasClassForPlayer(player, DialogPanel, OpenClass, false);
                hud.SetInputCaptureEnabled(player, false);
            }
            catch (Exception ex)
            {
                _plugin.DebugMode($"[SprayPanel] Failed to close for slot {player.Slot}: {ex}");
            }
        }

        if (_pages.Count == 0)
            Destroy();
    }

    private bool EnsureHud(out CCSCustomHudLayout hud)
    {
        if (_hud is { IsValid: true } existing)
        {
            hud = existing;
            return true;
        }

        hud = null!;
        string layout = _plugin.Config.PanoramaLayout.Trim();
        if (layout.Length == 0)
            return false;

        var spawned = Utilities.CreateEntityByName<CCSCustomHudLayout>("custom_hud_layout");
        if (spawned == null || !spawned.IsValid)
            return false;

        using var kv = new CEntityKeyValues();
        kv.SetString("layout", layout);
        kv.SetString("targetname", "spray_panorama_menu");
        spawned.DispatchSpawn(kv);

        _hud = hud = spawned;
        _appliedImages.Clear();
        return true;
    }

    private void Render(CCSCustomHudLayout hud, CCSPlayerController player)
    {
        int page = _pages[player.Slot];
        var entries = Entries;
        int pageCount = PageCount(entries.Count);
        SetText(hud, player, HeaderPanel, "spray_header", _plugin.Localizer["Menu_Header"]);
        SetText(hud, player, PagePanel, "spray_page", $"{page + 1} / {pageCount}");
        SetText(hud, player, ClosePanel, "spray_close", _plugin.Localizer["Menu_Close"]);

        var selection = _plugin.SprayManager!.GetSelection(player);
        if (!_appliedImages.TryGetValue(player.Slot, out var applied))
            _appliedImages[player.Slot] = applied = new string?[PageSize];

        for (int i = 0; i < PageSize; i++)
        {
            int card = i + 1;
            int index = page * PageSize + i;
            bool hasSpray = index < entries.Count;
            // Empty slots stay in the layout (transparent) so the panel keeps its size
            hud.SetHasClassForPlayer(player, $"{CardPrefix}{card}", EmptyClass, !hasSpray);
            if (!hasSpray)
                continue;

            var spray = entries[index];
            SetText(hud, player, $"{NamePrefix}{card}", $"spray_name_{card}", spray?.Name ?? _plugin.Localizer["Menu_Random"]);

            string imageClass = spray?.ImageClass ?? RandomImageClass;
            if (applied[i] is { } previous && previous != imageClass)
                hud.SetHasClassForPlayer(player, $"{ImagePrefix}{card}", previous, false);
            hud.SetHasClassForPlayer(player, $"{ImagePrefix}{card}", imageClass, true);
            applied[i] = imageClass;

            hud.SetHasClassForPlayer(player, $"{CardPrefix}{card}", SelectedClass, IsSelected(spray, selection));
        }

        hud.SetHasClassForPlayer(player, PrevPanel, ButtonOffClass, page == 0);
        hud.SetHasClassForPlayer(player, NextPanel, ButtonOffClass, page >= pageCount - 1);
    }

    // Written against the dialog and the label: the client's dialog-variable scope
    // resolution is not pinned down.
    private static void SetText(CCSCustomHudLayout hud, CCSPlayerController player, string panel, string variable, string value)
    {
        hud.SetDialogVariableStringForPlayer(player, DialogPanel, variable, value);
        hud.SetDialogVariableStringForPlayer(player, panel, variable, value);
    }

    private void OnHudClicked(CCSPlayerController player, CCSCustomHudLayout customLayout, string buttonId)
    {
        if (_hud is not { IsValid: true } hud || customLayout.Handle != hud.Handle)
            return;

        int slot = player.Slot;
        Server.NextFrame(() => HandleClick(slot, buttonId));
    }

    private void HandleClick(int slot, string buttonId)
    {
        try
        {
            var player = Utilities.GetPlayerFromSlot(slot);
            if (player is null || !player.IsValid || _hud is not { IsValid: true } hud || !_pages.TryGetValue(slot, out int page))
                return;

            if (buttonId == ClosePanel)
            {
                Close(player);
                return;
            }

            if (buttonId == PrevPanel || buttonId == NextPanel)
            {
                int next = Math.Clamp(page + (buttonId == NextPanel ? 1 : -1), 0, PageCount(Entries.Count) - 1);
                if (next != page)
                {
                    _pages[slot] = next;
                    Render(hud, player);
                }
                return;
            }

            if (buttonId.StartsWith(CardPrefix, StringComparison.Ordinal)
                && int.TryParse(buttonId.AsSpan(CardPrefix.Length), out int card)
                && card >= 1 && card <= PageSize)
            {
                int index = page * PageSize + card - 1;
                var entries = Entries;
                if (index >= entries.Count)
                    return;

                var spray = entries[index];
                _plugin.SprayManager!.Select(player, spray?.Name ?? Managers.SprayManager.RandomSelection);
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["SpraySelected", spray?.Name ?? _plugin.Localizer["Menu_Random"]]}");
                Close(player);
            }
        }
        catch (Exception ex)
        {
            _plugin.DebugMode($"[SprayPanel] Failed to handle click '{buttonId}' for slot {slot}: {ex}");
        }
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event.Userid is { IsValid: true } player)
            Close(player);

        return HookResult.Continue;
    }

    public void Destroy()
    {
        var hud = _hud;
        _hud = null;
        var slots = _pages.Keys.ToList();
        _pages.Clear();
        _appliedImages.Clear();

        if (hud is not { IsValid: true })
            return;

        try
        {
            // Release every cursor before the entity disappears
            foreach (int slot in slots)
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player is { IsValid: true })
                    hud.SetInputCaptureEnabled(player, false);
            }

            hud.AcceptInput("Kill");
        }
        catch (Exception ex)
        {
            _plugin.DebugMode($"[SprayPanel] Failed to kill the panel entity: {ex}");
        }
    }
}
