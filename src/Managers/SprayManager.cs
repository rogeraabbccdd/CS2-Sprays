using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Sprays.Models;
using Clientprefs.API;
using Microsoft.Extensions.Logging;

namespace CS2_Sprays.Managers;

public class SprayManager(CS2_Sprays plugin)
{
    public const string SoundEventsFile = "soundevents/game_sounds_player.vsndevts";
    private const string CookieName = "spray_selected";

    private readonly CS2_Sprays _plugin = plugin;

    // Selections are saved in the Clientprefs cookie "spray_selected" (spray name).
    // Without Clientprefs they only live here until the server restarts.
    private IClientprefsApi? _clientprefs;
    private int _cookieId = -1;
    private readonly Dictionary<ulong, string> _selections = new();

    // SteamID64 -> that player's spray on the map (one per player, a new one replaces it).
    // Position and material are kept so the spray can be put back after a round restart.
    private sealed class PlacedSpray(string material, Vector position, QAngle angle, CEnvDecal decal)
    {
        public string Material { get; } = material;
        public Vector Position { get; } = position;
        public QAngle Angle { get; } = angle;
        public CHandle<CEnvDecal> Decal { get; set; } = new(decal.EntityHandle.Raw);
    }

    private readonly Dictionary<ulong, PlacedSpray> _sprays = new();
    private readonly Dictionary<ulong, float> _lastSpray = new();

    private bool CookieReady => _clientprefs != null && _cookieId != -1;

    public void LoadClientprefs(IClientprefsApi? api, bool hotReload)
    {
        _clientprefs = api;
        if (api == null)
        {
            _plugin.Logger.LogWarning("Clientprefs not found, spray selections will not be saved.");
            return;
        }

        api.OnDatabaseLoaded += OnClientprefsDatabaseLoaded;

        // The database was loaded before this plugin on a hot reload
        if (hotReload)
            OnClientprefsDatabaseLoaded();
    }

    public void UnloadClientprefs()
    {
        if (_clientprefs != null)
            _clientprefs.OnDatabaseLoaded -= OnClientprefsDatabaseLoaded;
    }

    private void OnClientprefsDatabaseLoaded()
    {
        var api = _clientprefs;
        if (api == null) return;

        Task.Run(async () =>
        {
            _cookieId = await api.RegPlayerCookieAsync(CookieName, "Selected spray", CookieAccess.CookieAccess_Protected);
            if (_cookieId == -1)
                _plugin.Logger.LogError($"Failed to register the Clientprefs cookie '{CookieName}'.");
        });
    }

    public SprayModel? GetSelected(CCSPlayerController player)
    {
        string? name = CookieReady
            ? _clientprefs!.GetPlayerCookie(player, _cookieId)
            : _selections.GetValueOrDefault(player.SteamID);

        return string.IsNullOrEmpty(name) ? null : _plugin.Config.Sprays.FirstOrDefault(s => s.Name == name);
    }

    public void Select(CCSPlayerController player, SprayModel spray)
    {
        if (CookieReady)
            _clientprefs!.SetPlayerCookie(player, _cookieId, spray.Name);
        else
            _selections[player.SteamID] = spray.Name;
    }

    public void OnMapStart()
    {
        _sprays.Clear();
        _lastSpray.Clear();
    }

    // Round restarts remove every env_decal: put the sprays back, or forget them
    public void OnRoundPoststart()
    {
        if (_plugin.Config.ResetCooldownOnRoundStart)
            _lastSpray.Clear();

        foreach (var (steamId, placed) in _sprays.ToList())
        {
            // In case the decal survived the cleanup
            if (placed.Decal.Value is { IsValid: true } old)
                old.Remove();

            if (!_plugin.Config.KeepSpraysBetweenRounds)
                continue;

            var decal = CreateDecal(placed.Position, placed.Angle, placed.Material);
            if (decal != null)
                placed.Decal = new CHandle<CEnvDecal>(decal.EntityHandle.Raw);
            else
                _sprays.Remove(steamId);
        }

        if (!_plugin.Config.KeepSpraysBetweenRounds)
            _sprays.Clear();
    }

    public void ResetCooldown(CCSPlayerController player)
    {
        _lastSpray.Remove(player.SteamID);
    }

    public void TrySpray(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["MustBeAlive"]}");
            return;
        }

        var spray = GetSelected(player);
        if (spray == null)
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoSpraySelected"]}");
            return;
        }

        if (_lastSpray.TryGetValue(player.SteamID, out var last))
        {
            var left = last + _plugin.Config.SprayCooldown - Server.CurrentTime;
            if (left > 0)
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["Cooldown", Math.Ceiling(left)]}");
                return;
            }
        }

        var origin = pawn.AbsOrigin!;
        var eyePos = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        var eyeAngles = pawn.EyeAngles;

        var trace = Trace.TraceShape(eyePos, eyeAngles, pawn, new TraceOptions { InteractsWith = Masks.Solid });
        var hit = trace.HitPoint;
        if (!trace.DidHit() || Distance(eyePos, hit) > _plugin.Config.SprayDistance || trace.HitEntity() is { IsValid: true, DesignerName: "player" })
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["TooFar"]}");
            return;
        }

        var normal = trace.Normal;
        var position = new Vector(hit.X + normal.X, hit.Y + normal.Y, hit.Z + normal.Z);
        var angle = AnglesFromNormal(normal, eyeAngles.Y);

        var decal = CreateDecal(position, angle, spray.Material);
        if (decal == null)
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["SprayFailed"]}");
            return;
        }

        if (_sprays.TryGetValue(player.SteamID, out var old) && old.Decal.Value is { IsValid: true } oldDecal)
            oldDecal.Remove();

        _sprays[player.SteamID] = new PlacedSpray(spray.Material, position, angle, decal);
        _lastSpray[player.SteamID] = Server.CurrentTime;

        pawn.EmitSound(_plugin.Config.SpraySound);
    }

    // env_decal projects along its local -Z, so its up axis has to point out of the surface.
    // Walls get pitch 90 and floors pitch 0, so textures turned 90 degrees counterclockwise
    // (what tools/sync_sprays.py writes) show upright.
    private static QAngle AnglesFromNormal(Vector normal, float eyeYaw)
    {
        float pitch = MathF.Acos(Math.Clamp(normal.Z, -1f, 1f)) * 180f / MathF.PI;

        float yaw;
        if (MathF.Sqrt(normal.X * normal.X + normal.Y * normal.Y) < 0.01f)
            yaw = normal.Z > 0 ? eyeYaw + 180f : eyeYaw; // floor or ceiling: face the player
        else
            yaw = MathF.Atan2(normal.Y, normal.X) * 180f / MathF.PI;

        return new QAngle(pitch, yaw, 0);
    }

    private CEnvDecal? CreateDecal(Vector position, QAngle angle, string material)
    {
        /*
            Thanks to github.com/f3nixCodings for providing me updated way of creating decals.
        */
        try
        {
            using var keyValues = new CEntityKeyValues();
            var entity = Utilities.CreateEntityByName<CEnvDecal>("env_decal");
            if (entity == null) return null;

            keyValues.SetString("targetname", "player_spray");
            keyValues.SetString("material", material);

            entity.Width = _plugin.Config.SpraySize;
            entity.Height = _plugin.Config.SpraySize;
            entity.Depth = _plugin.Config.SprayDepth;
            entity.RenderOrder = 1;
            entity.RenderMode = RenderMode_t.kRenderNormal;
            entity.ProjectOnWorld = true;

            entity.Teleport(position, angle);
            entity.DispatchSpawn(keyValues);
            return entity;
        }
        catch (Exception error)
        {
            _plugin.DebugMode($"{error}");
            return null;
        }
    }

    private static float Distance(Vector a, Vector b)
    {
        float x = a.X - b.X, y = a.Y - b.Y, z = a.Z - b.Z;
        return MathF.Sqrt(x * x + y * y + z * z);
    }
}
