---
description: Vintage Story mod agent instructions — packaging, GuiComposer UI system, config, and testing conventions
alwaysApply: false
---

# Vintage Story Agent Instructions (`vs-agents.md`)

Manual include for new mod work. Example prompt:

> Create a new client mod X. Follow `vs-agents.md` for packaging, UI, and config rules.

Target stack unless the user says otherwise: **Vintage Story 1.22**, **.NET 10 (`net10.0`)**, author **thejimmyblaze**.

---

## 1. Prerequisites

- `VINTAGE_STORY` user env var → game install folder (contains `VintagestoryAPI.dll`), **not** `VintagestoryData`.
- .NET 10 **SDK** installed (`dotnet --list-sdks` must show 10.x).
- Prefer inspecting local refs under `./ref/*.zip` and unpacked cache under `%AppData%\Roaming\VintagestoryData\Cache\unpack\` before inventing patterns.
- Canonical hotkey GUI reference: **Alloy Calculator** (stock `GuiComposer`). Calendar/date reference: **Wall Calendar** + `IGameCalendar`. Lang month patches: **Elder Scrolls Calendar**.

```powershell
[Environment]::SetEnvironmentVariable("VINTAGE_STORY", "$Env:AppData\Vintagestory", "User")
```

---

## 2. Project / packaging (hard rules)

### Ship as one ModDB zip

- Output must be a single zip: `{modid}_{version}.zip`.
- Zip root contains only:
  - `modinfo.json`
  - `modicon.png` (optional but preferred)
  - `{modid}.dll`
  - `assets/{modid}/...`
  - optional `{modid}.deps.json` / `.pdb` (ok for local builds)
- **Never** ship extra DLLs (`cairo-sharp`, Harmony, etc.) inside the mod zip unless the user explicitly requires a library mod dependency.
- Game assemblies are **compile-only** references: `<Private>false</Private>`.
- Prefer **no** `cairo-sharp` reference. Use stock fonts (`WhiteDetailText`, `WhiteSmallText`, `WhiteSmallishText`). If bold/`FontWeight` is required, match Alloy Calculator: reference `$(VINTAGE_STORY)\Lib\cairo-sharp.dll` with `Private=false` only — still do **not** copy it into the zip.
- Install path for player-style tests: `%AppData%\Roaming\VintagestoryData\Mods\{modid}_{version}.zip`.
- **Never** leave a loose `Mods\{modid}\` folder beside the zip (causes duplicate modid / “assembly already loaded”).
- After replacing a compiled DLL/zip: user must **fully quit** Vintage Story (not just Leave world). SP keeps assemblies loaded for the process lifetime.
- Prefer forward-slash zip entry paths when packaging on Windows.

### `modinfo.json`

```json
{
  "type": "code",
  "side": "client",
  "modid": "examplemod",
  "name": "Example Mod",
  "authors": ["thejimmyblaze"],
  "description": "...",
  "version": "1.0.0",
  "dependencies": { "game": "1.22.0" }
}
```

Use `"side": "client"` for pure GUI/hotkey mods. Use `universal` only when server logic is required.

### Project shape

- `ModSystem` with `ShouldLoad(EnumAppSide.Client)` for client-only mods.
- Hotkey dialogs: `RegisterHotKey` + `Gui.RegisterDialog` + `GuiDialog.ToggleKeyCombinationCode`.
- Recompose UI in `OnGuiOpened` so world config / calendar / lang patches are current.
- Localize all user-facing strings under `assets/{modid}/lang/en.json`. Use `Lang.Get("{modid}:key", ...)`.
- **Do not** ship `game:month-*` overrides. Resolve vanilla/patched names with `Lang.Get("month-" + EnumMonth)` so calendar-name mods keep working.

---

## 3. UI system (hard rules)

### Prefer stock GuiComposer — no custom draw by default

Use the same conventional widgets as Alloy Calculator / handbook chrome:

| Use | Widget |
|-----|--------|
| Dialog chrome | `AddShadedDialogBG` + `AddDialogTitleBar` |
| Labels | `AddStaticText` |
| Live-updating labels | `AddDynamicText` + `SetNewText(..., autoHeight: false, forceRedraw: true)` |
| Recessed panels / day cells | `AddInset` |
| Progress | `AddStatbar` + `SetValues` / `SetLineInterval` |
| Right-aligned text | same font + `WithOrientation(EnumTextOrientation.Right)` **or** `AddStaticText(..., EnumTextOrientation.Right, ...)` |

**Forbidden unless the user explicitly asks:**

- Custom Cairo `AddDynamicCustomDraw` / `AddStaticCustomDraw` surfaces for whole UIs
- Shipping UI helper DLLs
- Inventing a second visual language (custom cards, neon, non-VS chrome)

Custom draw is only acceptable for small decorative icons after stock layout works, and must draw in **local** widget coordinates (not `bounds.drawX/drawY` on a 0,0 surface).

### Visual consistency tokens (replicate these)

Use these constants so future GUIs look like the same family:

```text
leftMargin          = 24
titleBarHeight      = GuiStyle.TitleBarHeight   // 30
topInsetAfterTitle  = 46                       // space from title bar bottom → first header row
afterHeaderGap      = 8
afterBarGap         = 8
afterSectionGap     = 6
infoRowHeight       = 22
detailLabelHeight   = 22
headerFont          = CairoFont.WhiteSmallishText()
body/detailFont     = CairoFont.WhiteDetailText().WithColor(FutureTextColor)
sectionLabelFont    = CairoFont.WhiteSmallText()  // colored when encoding categories
FutureTextColor     = { 0.95, 0.92, 0.84, 1.0 }
PastTextColor       = { 0.55, 0.50, 0.42, 0.9 }
TodayTextColor      = { 0.98, 0.86, 0.45, 1.0 }
SpringColor         = { 0.43, 0.74, 0.31, 1.0 }
SummerColor         = { 0.95, 0.73, 0.14, 1.0 }
AutumnColor         = { 0.90, 0.54, 0.17, 1.0 }
WinterColor         = { 0.70, 0.82, 0.92, 1.0 }
```

### Layout rhythm (negative space)

1. **Equal outer rhythm:** distance from the **top of the content panel** (below title bar) to the main graph/header block should match the distance from the **bottom of that graph/info block** to the next major section (e.g. grids).
   - Practical formula used successfully: `gridsY = graphBottom + dateY` where `dateY = titleBarHeight + topInset`.
2. Keep tight, even gaps between stacked header pieces (date → bar → season labels → info row): `8 / 8 / 6`.
3. Same-row left/right labels **must** share identical `Y` and `height`. Do not mix different font sizes or different vertical padding on that row.
4. Right-edge labels (e.g. `Day 42 of 108`, countdowns) align to the **same right content edge** as each other.
5. Avoid large empty void under the title bar while cramming later sections; spend the top inset intentionally, then mirror it below the header cluster.

### Text / highlight rules

- Secondary facts use **detail** font (`WhiteDetailText`), same size as `Day X of Y`.
- Do **not** enlarge “today” / emphasis with a bigger font family — same font, accent **color** only. Larger fonts break vertical centering in inset cells.
- Center day/cell numbers with pad derived from `GuiStyle.DetailFontSize`, e.g. `dayTextPad = max(1, (cell - DetailFontSize) / 2)`.
- Past values: dim/`PastTextColor`. Current: `TodayTextColor`. Future: `FutureTextColor`.
- Category markers may use short ASCII (`+ * ~ o`) plus color; keep them consistent across legend + section titles.

### Progress bars / seasons

- Stat bar fill and tick intervals must follow **real semantics**, not cosmetic equal quarters.
  - Example: if month grids start at May (spawn), the **season progress year** still starts at spring (March north / September south). May is late spring, not bar zero for “spring start”.
- Tick lines: `SetLineInterval(DaysPerMonth * 3)` for 3-month seasons when the bar domain is a full seasonal year.
- Hemisphere: use `calendar.GetHemisphere(playerPos)` / `GetSeason(playerPos)`; flip season→month mapping for south.

### Live-updating UI

- Singleplayer pauses game ticks while many dialogs are open. **Do not** rely on `RegisterGameTickListener` for countdown/UI refresh.
- Use `OnRenderGUI(float dt)` (real frame time) to:
  - decrement displayed real-time countdowns while paused
  - resync from `IGameCalendar` about once per second when `!capi.IsGamePaused`
- Update labels with `GetDynamicText(key).SetNewText(text, false, true, false)`.
- Dispose/recreate composers carefully; call `SingleComposer?.Dispose()` before rebuild.

### Dialog lifecycle

```csharp
api.Input.RegisterHotKey(code, Lang.Get("modid:open"), GlKeys.L, HotkeyType.GUIOrOtherControls);
api.Gui.RegisterDialog(new GuiDialogX(api));
// GuiDialog: ToggleKeyCombinationCode => code;
// OnGuiOpened => ComposeDialog();
```

---

## 4. Configurability & depending on configurations

### Prefer built-in configuration surfaces

| Concern | Preferred mechanism | Notes |
|--------|---------------------|-------|
| Hotkeys | `RegisterHotKey` | User-rebindable in Controls. Never hardcode “only L”. |
| World tempo / month length | `IGameCalendar` / worldconfig | Read at runtime (`DaysPerMonth`, `HoursPerDay`, `SpeedOfTime`, `CalendarSpeedMul`). |
| Display names | `Lang.Get` | Respect other mods’ lang patches. |
| Client-only toggles | Ask before adding ConfigLib | Default: no ConfigLib dependency for v1. |

### Rules when depending on configuration

1. **Read live, don’t cache across sessions in UI code.** Rebuild model + composer on `OnGuiOpened`.
2. **Honor world settings automatically.** Grids, bars, and labels must resize/recount for `DaysPerMonth` 3–30 (and whatever the calendar reports).
3. **Document reload requirements.** `/worldconfig ...` often needs a **relog/restart** before `IGameCalendar` changes; say so in README/test plan.
4. **Real-time estimates** (HH:MM:SS until event) must use:
   - remaining game days/hours from calendar, and
   - real seconds ≈ `gameHours * 3600 / (SpeedOfTime * CalendarSpeedMul)` (defaults yield ~48 real minutes/day).
5. **No silent assumptions** about 9-day months or northern hemisphere — always query the API.
6. **Optional config files** only if the user asks. If added:
   - keep defaults sensible with zero config file present
   - never require a second DLL to read config
   - validate ranges; fall back instead of crashing
7. **Dependencies** in `modinfo.json` only for true requirements. Do not add Harmony/ConfigLib “just in case”.

---

## 5. Domain notes discovered (calendar / time)

- Enum months: January=1 … December=12. `DayOfYear` is 0-based.
- New worlds spawn **1 May** (Elder Scrolls name: Second Seed). Month **grids** may start there and wrap; **season bars** still use spring start (March/Sep).
- Northern seasons: Mar–May spring, Jun–Aug summer, Sep–Nov autumn, Dec–Feb winter.
- “Days until winter” → target winter; if currently winter → “days until spring”.
- Character panel date format pieces: day, localized month, year.

---

## 6. Build / test workflow

- Provide `build.ps1` that: `dotnet build -c Release` → zip `Releases/{modid}_{version}.zip` → copy into `VintagestoryData\Mods` → remove leftover loose `Mods\{modid}` if possible.
- After install: **full game restart**, then Mod Manager check, then in-world hotkey test.
- If logs show `Assembly with same name is already loaded`: quit process, ensure only one copy of the mod exists in Mods, relaunch.
- Logs: `%AppData%\Roaming\VintagestoryData\Logs\client-main.log`.

### Conventional in-game checklist

1. Mod listed and enabled.
2. Hotkey opens/closes dialog; Controls rebind works.
3. UI matches spacing/font rules above (no clipped/misaligned same-row labels).
4. Values match vanilla panels / worldconfig (date, days/month, hemisphere).
5. Live timers tick while dialog stays open (including SP pause).
6. Compat smoke: lang-patch mods still rename displayed months.
7. No extra DLLs inside the installed zip.

---

## 7. Agent behavior checklist

When implementing a new VS mod under these instructions:

- [ ] Scaffold `net10.0` csproj with `Private=false` game refs only
- [ ] Client `ModSystem` + hotkey `GuiDialog` if UI
- [ ] Stock GuiComposer chrome + spacing tokens
- [ ] Lang file for all strings; no illegal game key overrides
- [ ] Runtime reads of calendar/world config; recompose on open
- [ ] Zip-only install artifact; no loose Mods folder; no shipped cairo
- [ ] README with build, restart, and config/relog notes
- [ ] Verify against logs after first load

**Do not** invent a custom UI framework, ship helper DLLs, or ignore world/lang configuration that the game already exposes.
