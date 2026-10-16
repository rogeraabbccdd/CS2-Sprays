# sync_sprays.py

Syncs a folder of spray images into the workshop addon and the plugin config. Each image becomes a spray material, a menu picture, a line in `sprays_images.css` and an entry in `"Sprays"`.

## Why the pictures live in the addon

The spray menu cannot load images at runtime:

- The client's custom HUD validator only allows `Panel`, `Label` and `Button` with basic attributes (`id`, `class`, `text`, `hittest`). There is no `Image` panel and no `html` attribute.
- Panorama CSS `url()` only accepts `file://`, `panel://` and `s2r://`. `http(s)` URLs fail to compile.

So the plugin gives every card a class (`.spray-i-<Image>`), and the addon's `panorama/styles/custom_game/sprays_images.css` maps each class to a compiled picture. The spray itself is a decal material, so it has to be in the addon too.

## Requirements

- Python 3.10 or newer
- Pillow: `pip install pillow`
- CS2 Workshop Tools on Windows. The addon has to be in `<CS2>/content/csgo_addons/<addon>`, and `--compile` uses `<CS2>/game/bin/win64/resourcecompiler.exe`.

## Quick start

1. Put the spray images in a folder, e.g. `sprays/`. Use upright pictures; any size and aspect works, and transparency is kept. The file name becomes the menu name:
   ```
   sprays/
     Cat.png
     Smile Face.png
     gg.jpg
   ```
2. Copy the server's `configs/plugins/CS2-Sprays/CS2-Sprays.json` next to the folder (or pass its path with `--config`).
3. Sync and compile:
   ```
   python tools/sync_sprays.py --images sprays --config CS2-Sprays.json --addon "<CS2>/content/csgo_addons/<addon>" --compile
   ```
4. Publish the addon with the Workshop Tools.
5. Copy the config back to the server, reload the plugin and change map. The new materials are precached on map start.

`<CS2>` is the game's install folder, e.g. `C:/Program Files (x86)/Steam/steamapps/common/Counter-Strike Global Offensive`.

Preview the changes without writing anything:

```
python tools/sync_sprays.py --images sprays --config CS2-Sprays.json --addon "<CS2>/content/csgo_addons/<addon>" --dry-run
```

## Options

| Option | Description |
| --- | --- |
| `--images` | Folder with the spray images: png, jpg, jpeg, webp, bmp, tga (required) |
| `--config` | Path to the plugin config. Default: `CS2-Sprays.json` in the current folder |
| `--addon` | The addon's content folder (required) |
| `--size` | Material texture size, a power of 2. Default: `512` |
| `--folder` | Material folder under `materials/`. Default: `sprays` |
| `--prune` | Remove config sprays in `materials/<folder>/` whose image is no longer in `--images` |
| `--compile` | After writing, compile the new materials and `sprays_images.css` with `resourcecompiler.exe` |
| `--force` | Regenerate sprays that already exist in the addon (e.g. after editing an image) |
| `--dry-run` | Show what would change without writing anything |

## What it does

Each image gets an id: the file name in lower case, with characters other than `a-z`, `0-9`, `_` and `-` replaced by `_`. `Smile Face.png` becomes `smile_face`.

1. **Copies the menu files.** `sprays.xml` and `sprays.css` are copied from this repo's `addon/` folder into your addon when they are missing. Existing ones are not touched.
2. **Writes the sprays.** New images only; existing sprays are skipped unless you pass `--force`.
   - `materials/<folder>/<id>.png`: the image on a black square, turned 90 degrees counterclockwise, so it shows upright on walls.
   - `materials/<folder>/<id>_trans.png`: the translucency mask (white = visible), taken from the image's alpha.
   - `materials/<folder>/<id>.vmat`: decal material with the same settings as the example.
   - `panorama/images/custom_game/sprays/<id>.png`: the 128x128 menu picture, upright.

   An image that cannot be read is reported with `!` and skipped.
3. **Updates the config.**
   - Images that are not in `"Sprays"` yet are appended as `{ "Name": <file name>, "Material": "materials/<folder>/<id>.vmat", "Image": <id> }`.
   - Entries are matched by `Image`. Names, order and other sprays are kept, so you can rename a spray in the config and later syncs will not overwrite it.
   - Sprays in `materials/<folder>/` whose image is gone are only reported (lines starting with `?`). `--prune` removes them.
   - Before any change, the original file is copied to `<config>.bak`.
4. **Writes `panorama/styles/custom_game/sprays_images.css`.** One class per PNG in `panorama/images/custom_game/sprays/`.
5. **`--compile`** compiles the menu files copied in step 1, the new `.vmat` files (with their textures) and the stylesheet (with its pictures). Each line should read `N compiled, 0 failed`.

## Updating sprays

- **Add:** put the image in the folder and run the quick start command.
- **Change a picture:** edit the image and run with `--force`.
- **Rename:** change `"Name"` in the config. Players' choices are saved by name, so players who chose that spray have to choose it again.
- **Remove:** delete the image and run with `--prune` (see Cleanup for the addon files).

## Compiling manually

In PowerShell:

```powershell
& "<CS2>\game\bin\win64\resourcecompiler.exe" -nop4 -i "<CS2>\content\csgo_addons\<addon>\panorama\styles\custom_game\sprays_images.css"
& "<CS2>\game\bin\win64\resourcecompiler.exe" -nop4 -i "<CS2>\content\csgo_addons\<addon>\materials\sprays\<id>.vmat"
```

- Add `-f` to force a full recompile. Use it when a changed image still shows the old picture in game, because `--compile` may report `0 compiled` for a file it thinks is up to date.
- After changing `sprays.xml` or `sprays.css`, compile that file too. Compiling the layout does not recompile a changed stylesheet, so compile each changed file on its own.

## Cleanup

`--prune` only edits the config. It does not delete files. The material, textures and menu picture of a removed spray stay in the addon, and its class stays in `sprays_images.css` while the menu picture exists. They cause no errors, but they make the addon bigger. To remove them, delete the files with that id from:

- `<CS2>/content/csgo_addons/<addon>/materials/<folder>/`
- `<CS2>/content/csgo_addons/<addon>/panorama/images/custom_game/sprays/`
- the same folders under `<CS2>/game/csgo_addons/<addon>/` (compiled files)

Then run the script again to rewrite the stylesheet.

## Pitfalls

- **Do not edit `sprays_images.css` by hand.** The script rewrites it from the PNGs in `panorama/images/custom_game/sprays/`. To add a picture by hand, put the PNG in that folder and run the script.
- **Copy `sprays_images.css` from the repo only together with the example picture.** It points to `example.png`; without that file the compile fails. The script writes its own anyway.
- **The example spray** in the repo's `addon/` folder is generated from `tools/example/Example.png`: `python tools/sync_sprays.py --images tools/example --addon addon --config <any json> --force`.
- **Images inside the addon must be referenced as `file://{images}/...png`.** That is a strong reference, so compiling the stylesheet also compiles the PNG. An `s2r://..._png.vtex` reference is weak, and the PNG never gets compiled. The script already does this.
- **Two images with the same id** (e.g. `Cat.png` and `cat.jpg`) cannot both be used. The second one is skipped with a `!` line.
- **`--addon` cannot be empty.** An empty path would mean the current folder, so the script refuses to run.
- **Python 3.14 alpha builds can crash.** If the script exits without output, run it with a stable Python (3.10–3.13).
