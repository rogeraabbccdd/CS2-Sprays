# CS2-Sprays

Player sprays for CS2: pick a spray from a menu with pictures, then spray it on walls and floors.

Based on [CS2-Poor-MapAdvertisements](https://github.com/Letaryat/CS2-Poor-MapAdvertisements) by [Letaryat](https://github.com/Letaryat).

<p align="center">
    <img src="img/menu.jpg" width="49%">
    <img src="img/spray.jpg" width="49%">
</p>

<sub>Sprays in the screenshots: [SapphyStickers Collection](https://yueou.booth.pm/items/7076197) by 蛸乃ゆえ (illustration: 刺鼻). Not included in this repository.</sub>

## [✨] Features
- Panorama menu with pictures (`!sprays`), click to choose
- Spray where you look (`!spray`, or bind `css_spray`), with the spray can sound
- Random spray option
- One spray per player, kept between rounds
- Cooldown, reset on round start and on kill
- Optional: spray with E
- Choices saved with Clientprefs

## [📌] Requirements
- CounterStrikeSharp 1.0.374+
- [Clientprefs](https://github.com/Cruze03/Clientprefs) v1.0.7+ (to save choices)
- Your own workshop addon, built with the CS2 Workshop Tools and mounted with e.g. [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager). Players without it see no menu and no sprays.

## [📌] Installation
1. Download `CS2-Sprays-<version>.zip` from [Releases](https://github.com/rogeraabbccdd/CS2-Sprays/releases) and extract it into `game/csgo/`.
2. Set up the workshop addon (below).
3. Restart the server. The config is created in `configs/plugins/CS2-Sprays/`.

## [🎨] Workshop addon
1. Copy the contents of `CS2-Sprays-addon-<version>.zip` into `<CS2>/content/csgo_addons/<your addon>/`. It has the menu and an example spray.
2. Add your sprays with [`tools/sync_sprays.py`](tools/README.md):
   ```
   python tools/sync_sprays.py --images <image folder> --config CS2-Sprays.json --addon "<CS2>/content/csgo_addons/<your addon>" --compile
   ```
   It makes the materials and menu pictures from your images and adds them to the config.
3. Publish the addon in the Workshop Tools, then update the server config.

See [tools/README.md](tools/README.md) for details and for adding sprays by hand.

## [📝] Configuration
| Option | Default | Description |
| --- | --- | --- |
| Permission | `""` | Flag needed to use sprays. Empty = everyone |
| Sprays | Example | `Name` (unique), `Material` (`.vmat`), `Image` (picture id) |
| Panorama Layout | `panorama/layout/custom_game/sprays.xml` | Menu layout in the addon |
| Spray Size | `64` | Width and height |
| Spray Depth | `12` | Decal projection depth |
| Spray Distance | `150` | Max distance to the wall |
| Spray Cooldown | `30` | Seconds between sprays |
| Spray Sound | `SprayCan.Paint` | Sound event |
| Random Spray | `true` | Random card in the menu; default for new players |
| Keep Sprays Between Rounds | `true` | Put sprays back after a round restart |
| Reset Cooldown On Round Start | `true` | |
| Reset Cooldown On Kill | `true` | |
| Spray With Use Key | `false` | Spray with E (E also defuses and opens doors) |
| Debug Mode | `false` | Log errors |

## [⌨️] Commands
| Command | Description |
| --- | --- |
| `!sprays` / `css_sprays` | Open the spray menu |
| `!spray` / `css_spray` | Spray |

## [🔧] Building
```
dotnet build CS2-Sprays.sln -c Release
```
Needs the .NET 10 SDK. Output: `src/bin/Release/net10.0/`. The first build downloads `ClientprefsApi.dll` into `lib/` to compile against; it is not shipped with the plugin.

## [❤️] Credits
- [Letaryat](https://github.com/Letaryat) - [CS2-Poor-MapAdvertisements](https://github.com/Letaryat/CS2-Poor-MapAdvertisements), where this project started
- [Cruze03](https://github.com/Cruze03) - [Clientprefs](https://github.com/Cruze03/Clientprefs)
- [f3nixCoding](https://github.com/f3nixCodings) - env_decal keyvalues
