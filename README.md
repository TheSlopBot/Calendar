# Calendar Handbook

Client-side Vintage Story 1.22 mod. Press **L** (rebindable in Controls) to open a year calendar: current date, season progress, and a grid for every day of every month.

Month names come from `Lang.Get("month-January")` … so calendar-name mods such as Elder Scrolls Calendar apply automatically.

The UI uses the same GuiComposer widgets as Alloy Calculator (`AddShadedDialogBG`, `AddStaticText`, `AddInset`, `AddStatbar`). Install is a single ModDB-style zip (`modinfo.json`, `modicon.png`, `calendarhandbook.dll`, `assets/`). No extra libraries.

Month grids start on **May / Second Seed** (new-world spawn) and wrap through April. The year progress bar follows the **seasonal year** starting at the first month of spring (March north / September south), so Second Seed sits two-thirds through spring. Day counts and grid size come from the world’s `DaysPerMonth` when you open the dialog (`/worldconfig daysPerMonth` needs a relog).

## Requirements

- Vintage Story 1.22 / .NET 10
- `VINTAGE_STORY` user environment variable pointing at the game install (the folder with `VintagestoryAPI.dll`, not `VintagestoryData`)

```powershell
[Environment]::SetEnvironmentVariable("VINTAGE_STORY", "$Env:AppData\Vintagestory", "User")
```

Restart the IDE/terminal after setting it.

## Build / install

```powershell
./build.ps1
```

That produces `Releases/calendarhandbook_1.0.0.zip` and copies it into `%AppData%\Roaming\VintagestoryData\Mods`, the same way ModDB zips are installed.

Restart Vintage Story after copying. If an old loose `Mods\calendarhandbook\` folder exists, delete it so only the zip is loaded.

## Test

1. Main menu → Mod Manager: **Calendar Handbook** listed.
2. Load a survival world (default 9 days/month).
3. Press **L**: dialog opens; **L** / close X closes it.
4. Header matches character panel **C** date.
5. Year stat bar is spring→winter; on 6 Sun's Height it should be about halfway through summer.
6. Under the seasons: days until winter/spring on the left, live HH:MM:ss countdown on the right.
7. Month grids start at May / Second Seed and wrap to April; past days dim, today highlighted and centered.
8. Optional: `/worldconfig daysPerMonth 20`, relog, reopen **L** — grids should show 20 days.
9. Optional: enable Elder Scrolls Calendar and confirm month titles change.
