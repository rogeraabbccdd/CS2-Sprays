# sync_sprays.py

Turns a folder of images into sprays: materials and menu pictures in your workshop addon, and entries in the plugin config.

## Requirements

- Python 3.10+ with Pillow (`pip install pillow`)
- CS2 Workshop Tools on Windows, with the addon in `<CS2>/content/csgo_addons/<addon>`

`<CS2>` is the game folder, e.g. `C:/Program Files (x86)/Steam/steamapps/common/Counter-Strike Global Offensive`.

## Quick start

1. Put upright images in a folder (any size, transparency is kept). The file name becomes the menu name.
   ```
   sprays/
     Cat.png
     Smile Face.png
   ```
2. Copy the server's `configs/plugins/CS2-Sprays/CS2-Sprays.json` next to it.
3. Run:
   ```
   python tools/sync_sprays.py --images sprays --config CS2-Sprays.json --addon "<CS2>/content/csgo_addons/<addon>" --compile
   ```
4. Publish the addon in the Workshop Tools.
5. Copy the config back to the server, reload the plugin and change map.

Add `--dry-run` to see the changes without writing anything.

## Options

| Option | Description |
| --- | --- |
| `--images` | Image folder (png, jpg, webp, bmp, tga). Required |
| `--addon` | Addon content folder. Required |
| `--config` | Plugin config. Default: `CS2-Sprays.json` |
| `--compile` | Compile the new files with the Workshop Tools' `resourcecompiler.exe` |
| `--force` | Regenerate sprays that already exist |
| `--prune` | Remove config entries whose image is gone |
| `--dry-run` | Show changes only |
| `--size` | Texture size, a power of 2. Default: `512` |
| `--folder` | Folder under `materials/`. Default: `sprays` |

## What it writes

Each image gets an id from its file name: lower case, other characters than `a-z 0-9 _ -` become `_` (`Smile Face.png` → `smile_face`).

- `materials/sprays/<id>.vmat`, `<id>.png`, `<id>_trans.png`: the spray material
- `panorama/images/custom_game/sprays/<id>.png`: the menu picture
- `panorama/styles/custom_game/sprays_images.css`: rewritten from all menu pictures
- The menu files (`sprays.xml`, `sprays.css`, `sprays_random.png`), only if they are missing
- New entries in `"Sprays"`, with a `.bak` copy of the old config

Existing sprays are skipped (use `--force` to redo them), and config entries are matched by `Image`, so names you change in the config are kept.

## Updating sprays

- **Add:** put the image in the folder and run again.
- **Change a picture:** edit the image and run with `--force`.
- **Rename:** change `"Name"` in the config. Players who chose that spray have to choose it again.
- **Remove:** delete the image and run with `--prune`. This only edits the config; delete the spray's files from the addon yourself.
- **Plugin update:** copy the new menu files from the addon zip over the old ones and compile them.

## Adding sprays by hand

1. Make the material like `materials/sprays/example.*`: a power-of-2 texture turned 90° counterclockwise, a translucency mask (white = visible), and a `.vmat` using `csgo_projected_decals.vfx`.
2. Put an upright 128x128 menu picture in `panorama/images/custom_game/sprays/`, e.g. `logo.png`, and add to `sprays_images.css`:
   ```css
   .spray-i-logo { background-image: url("file://{images}/custom_game/sprays/logo.png"); }
   ```
3. Add `{ "Name": "Logo", "Material": "materials/sprays/logo.vmat", "Image": "logo" }` to `"Sprays"`.
4. Compile and publish.

## Compiling manually

```powershell
& "<CS2>\game\bin\win64\resourcecompiler.exe" -nop4 -i "<path to the changed file>"
```

Compile each changed file on its own: the `.vmat`, `sprays_images.css`, `sprays.css` or `sprays.xml`. Add `-f` if the game still shows an old picture.

## Why the pictures are in the addon

The custom HUD only allows `Panel`, `Label` and `Button`, and panorama CSS cannot load `http(s)` images. So each menu card gets a class (`.spray-i-<Image>`) and `sprays_images.css` maps it to a picture compiled into the addon.
