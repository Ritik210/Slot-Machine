# Slot-Prototype

A Unity slot-machine prototype built to explore slot mechanics end to end: a data-driven math backend, a decoupled event-driven presentation layer, live RTP statistics, and editor tooling for authoring reel strips and paylines.

The included game is a generic **6×4 Ways-to-Win** slot with **tumbling (cascading) reels** and a **Free Spins** bonus. All math lives in one XML file, so the same code can run a different grid, paytable or reel strips without changes.

---

## 🎮 Play it

> **Playable build:** https://ritik210.github.io/Slot-Machine/Build/
>
> <!-- Replace the placeholder above with the hosted WebGL / itch.io URL, e.g.
>      **Playable build:** https://your-host.example.com/slot-prototype  -->

If no link is present, open the project in Unity and press **Play** in `Assets/Scenes/Proto_6x4.unity` (see [Getting Started](#-getting-started)).

---

## Table of contents

- [At a glance](#-at-a-glance)
- [Game rules](#-game-rules)
- [Getting started](#-getting-started)
- [Project structure](#-project-structure)
- [Architecture](#-architecture)
- [Configuration (XML)](#-configuration-xml)
- [Simulation & live stats](#-simulation--live-stats)
- [Editor tools](#-editor-tools)
- [Adapting the prototype](#-adapting-the-prototype)
- [Known issues & legacy code](#-known-issues--legacy-code)
- [Tech stack](#-tech-stack)

---

## 📌 At a glance

| | |
|---|---|
| **Engine** | Unity `6000.3.15f1` (Unity 6), Universal Render Pipeline 17.3 |
| **Language** | C# |
| **Main scene** | `Assets/Scenes/Proto_6x4.unity` (only scene in Build Settings) |
| **Grid** | 6 reels × 4 rows (configurable; 5×3, 6×5, 6×8 layout presets included) |
| **Win evaluation** | Ways-to-Win, left → right, minimum 3 of a kind, Wild substitutes |
| **Base bet** | 20 credits = $1.00; selectable bets $0.50 / $1 / $2 / $5 / $10 / $20 |
| **Config source** | `Assets/Scripts/SlotConfig.XML` |

---

## 🎰 Game rules

### Symbols

| ID | Name | Role |
|---|---|---|
| 0 | `B` | Blank / unused |
| 1 | `SCATTER` | 3+ anywhere trigger Free Spins and pay a scatter award |
| 2 | `Wild` | Substitutes for all paying symbols; pays 500 for 6-of-a-kind |
| 3–6 | `High1`–`High4` | High-pay symbols |
| 7–10 | `Low1`–`Low4` | Low-pay symbols |
| 11 | `EMPTY` | Internal — marks a removed cell during a tumble |

### Base game
- Each spin picks one of two **base reel sets** by weight (`BaseGame0` / `BaseGame1`, 50/50).
- Wins are evaluated as **Ways**: matching symbols (or Wilds) on consecutive reels from reel 1, minimum 3 reels. Payout = paytable value × bet multiplier.

### Tumbling reels (cascades)
- Every winning symbol is removed, the remaining symbols drop under gravity, and new symbols are pulled from the reel strip to refill from the top.
- The screen is re-evaluated and the cascade continues while there is a win. All tumble steps are returned in one `SpinResult` and animated sequentially, with the running win shown on the UI.

### Free Spins
- **3+ Scatters** anywhere award **15 Free Spins** and a scatter pay (3/4/5/6 scatters → 20 / 40 / 80 / 100 credits × bet multiplier).
- Free spins use the dedicated `FreeGame0` reel set. **3+ Scatters during Free Spins retrigger +5 spins.**
- No bet is deducted during Free Spins; the session total is shown in an exit popup.

### Player controls (Proto_6x4 UI)
- **Spin**, **Auto-spin** toggle, **Reset** (restores starting balance and stats)
- **Bet dropdown** ($0.50 – $20)
- **Speed dropdown** (1× / 2× / 4× animation speed)
- **Force Free Spin** debug option (one-shot: places 3–6 scatters on the next base spin) — `Server.SetForceFreeSpin(int)`

---

## 🚀 Getting started

### Prerequisites
- **Unity 6000.3.15f1** (install via Unity Hub; other Unity 6 patch versions will usually prompt a safe upgrade)
- Visual Studio 2022 / Rider (optional, `.vsconfig` included)

### Run in editor
1. Clone the repo and open the folder in Unity Hub.
2. Let Unity import packages (`Library/` is git-ignored and regenerates).
3. Open **`Assets/Scenes/Proto_6x4.unity`**.
4. Press **Play**. The starting balance is set on the `Server` object in the scene.

### Build
`File → Build Settings` → the only enabled scene is `Proto_6x4`. **WebGL** is the natural target for a shareable playable link (paste it at the top of this file).

---

## 🗂 Project structure

Only tracked, project-specific folders are listed (Unity's `Library/`, `Temp/`, `Logs/`, `obj/` etc. are git-ignored).

```
Slot-Prototype/
├─ Assets/
│  ├─ Scenes/
│  │  ├─ Proto_6x4.unity            ← current 6×4 prototype (build scene)
│  │  └─ GameScene.unity            ← older 5×3 test scene (uses Config.XML)
│  ├─ Scripts/
│  │  ├─ Backend/                   ← pure game math, no rendering
│  │  │  ├─ Server.cs               ← XML parsing, RNG, spin generation, free spins, RTP stats, simulation
│  │  │  ├─ Tumbler.cs              ← cascade engine: remove → gravity → refill → re-evaluate
│  │  │  ├─ SpinResult.cs           ← DTO returned per spin (screens, tumbles, wins, live stats)
│  │  │  ├─ WinLineInfo.cs          ← one winning way/line
│  │  │  └─ Utlis/
│  │  │     ├─ ISlotMode.cs         ← interface for win-evaluation strategies
│  │  │     ├─ WaysUtil.cs          ← Ways-to-Win evaluation (recursive path builder)
│  │  │     └─ LinesUtil.cs         ← Payline evaluation (20 fixed 5-reel lines) + wild handling
│  │  ├─ Core/
│  │  │  ├─ SlotGameController.cs   ← game loop: spin, auto-spin, free-spin chaining, reset
│  │  │  ├─ SpinResultVisualizer.cs ← turns a SpinResult into a timed event/animation sequence
│  │  │  ├─ BetController.cs        ← bet dropdown → Server.SetBet
│  │  │  ├─ UIInteractionManager.cs ← enable/disable UI controls
│  │  │  ├─ SlotModeType.cs         ← BaseGame / FreeSpin / Respin enum
│  │  │  ├─ Attributes/ReadOnlyAttribute.cs
│  │  │  └─ Events/                 ← event bus + all event types (see Architecture)
│  │  ├─ Frontend/
│  │  │  ├─ Reelmanager.cs          ← owns all reels; orchestrates spin/tumble animations
│  │  │  ├─ ReelView.cs             ← one reel column; gravity / drop-in easing
│  │  │  ├─ ReelSpinAnimator.cs     ← continuous spin loop and stop-snap per reel
│  │  │  ├─ SymbolView.cs           ← one cell: sprite + win highlight VFX
│  │  │  ├─ SlotUIManager.cs        ← balance / win / FS counter / feature labels / live stats text
│  │  │  ├─ SpinSpeedController.cs  ← global 1×/2×/4× animation speed
│  │  │  ├─ SlotLayoutConfig.cs     ← ScriptableObject: reels × rows + flat-index helpers
│  │  │  └─ Layouts/                ← Classic5x3, 6x4, 6x5, 6x8 layout assets
│  │  ├─ Tools/
│  │  │  ├─ ReelSetData.cs          ← ScriptableObject holding pasted reel-strip spreadsheets
│  │  │  └─ MultiPaylineEditorTool.cs ← visual payline designer (editor-only)
│  │  ├─ SlotConfig.XML             ← ACTIVE math config (6 reels)
│  │  └─ Config.XML                 ← older 5-reel config for GameScene
│  ├─ Editor/
│  │  ├─ ReelSetDataEditor.cs       ← "Parse and Transpose Reels" → XML <Reel> blocks
│  │  ├─ SpriteExtractor.cs         ← Tools ▸ Extract Sprites From Atlas
│  │  └─ Drawers/ReadOnlyDrawer.cs
│  ├─ Prefabs/                      ← FeatureText label, Glow win highlight
│  ├─ Effects/                      ← Glow sprite, highlight/blink AnimationClips & Animator controllers
│  ├─ Sprites/                      ← symbol art, backgrounds, reel frame, UI icons
│  ├─ ReelsParsing.asset            ← empty ReelSetData scratchpad for the reel-strip tool
│  ├─ InputSystem_Actions.inputactions
│  └─ TextMesh Pro/
├─ Packages/manifest.json
├─ ProjectSettings/
└─ .gitignore                       ← standard Unity template
```

---

## 🏗 Architecture

Three layers that never reference each other's internals directly; a **typed event bus** and a single **`SpinResult`** DTO carry data between them.

```
        ┌──────────────────────────┐
 Spin   │   SlotGameController     │  ← button / auto-spin / free-spin chaining
 ─────▶ │ (Core)                   │
        └───────────┬──────────────┘
                    │ server.GenerateSpinResponse()
                    ▼
        ┌──────────────────────────┐        ┌───────────────┐
        │   Server  (Backend)      │───────▶│  Tumbler      │  cascade loop
        │  XML config · RNG        │◀───────│               │
        │  ways eval · RTP stats   │        └───────────────┘
        └───────────┬──────────────┘
                    │ SpinResult (screen, tumbles[], wins, stats)
                    ▼
        ┌──────────────────────────┐
        │  SpinResultVisualizer    │  coroutine "director"
        └───────────┬──────────────┘
                    │ raises events with delays
                    ▼
        ┌──────────────────────────┐
        │      SlotEventHub        │  singleton, type-keyed pub/sub
        └───┬─────────┬────────┬───┘
            ▼         ▼        ▼
     Reelmanager  SymbolView  SlotModeEventDispatcher / SlotUnityEventRouter
     ReelView     (highlights)  (mode enter/exit → UnityEvents for designers)
     ReelSpinAnimator
```

### One spin, end to end
1. `SlotGameController.StartSpin()` → `Server.GenerateSpinResponse()` deducts the bet, picks a reel set by weight, rolls stop positions and builds the landed screen.
2. `Tumbler.Do()` resolves every cascade synchronously and returns the list of `TumbleInfo` steps; scatters are counted on the final screen and free spins are triggered / retriggered.
3. The `SpinResult` (final screen, tumble chain, wins, balances, live stats) is handed to `SpinResultVisualizer.VisualSequence()`.
4. The visualizer raises `SpinStartEvent`, then a `ReelStopEvent` per reel showing the **landed** screen (pre-cascade), then for each tumble step: highlight wins → fade → show holes → animate drop + refill → pause.
5. It raises `FreeSpinEvent` if the game is in free spins (the controller auto-starts the next spin), then `SpinEndEvent`, which updates live stats and free-spin popups and re-arms the spin button.

### Backend (deterministic math)
- **`Server`** parses the XML on `Start()`, owns balance/bet state, selects reel sets and stop positions with weighted RNG, delegates win evaluation to an **`ISlotMode`** (`WaysUtil` here; `LinesUtil` for payline games), runs the **`Tumbler`**, handles scatter / free-spin / retrigger logic, and accumulates RTP and hit-rate statistics that are attached to every `SpinResult`.
- **`Tumbler.Do()`** loops: evaluate wins → snapshot → remove winning symbols (`RemoveWinningSymbols`) → gravity (`DropSymbolsInBoard` computes each survivor's landing row, `SettleSymbols` places only real symbols onto an EMPTY-filled grid) → refill from the reel strip by walking `stopPositions` backwards (`ComputeTumbleScreenSymbols`, one new symbol per hole, merged into the top rows) → repeat while the step paid. Each iteration is captured as a **`TumbleInfo`** (landed / withHoles / settled / merged grids, move data, wins), which the visualizer replays step by step.
- Grids are `List<List<int>>` indexed `[reel][row]`; win positions use a **flat index** `row * reels + reel`.
- Symbol IDs are defined once in `Server.SymbolNames` and must match the `<Paytable>` order in the XML.
- `Server.simulationMode` runs `sampleSize` spins headlessly at startup and prints an RTP report (see below).

### Core (orchestration)
- **`SlotGameController`** guards against double spins, calls the server, hands the result to the UI (`OnSpinStart`) and starts `SpinResultVisualizer.VisualSequence`. It subscribes to `SpinEndEvent` (live stats, free-spin popups) and `FreeSpinEvent` (auto-start the next free spin after a delay). Also implements auto-spin and `ResetAll()`.
- **`SpinResultVisualizer`** is the animation director described above. All step timings (`reelStopDelay`, `tumbleWinHighlightDuration`, `tumbleRemoveDelay`, …) are inspector-tunable and scaled by `SpinSpeedController`. The running win and balance are pushed to the UI after every cascade step, then synced to the server's authoritative values at the end.

### Events (`Assets/Scripts/Core/Events`)
`SlotEventHub` is a `DontDestroyOnLoad` singleton with `Subscribe<T>`, `Unsubscribe<T>` and `Raise<T>(evt)`; every `SlotEvent` carries an optional `delaySeconds`, and dispatch happens in a coroutine.

| Event | Payload | Purpose |
|---|---|---|
| `SpinStartEvent` | — | Start reel spin loops; clear highlights |
| `ReelStopEvent` | `reelIndex`, `symbols` | Stop one reel on its final column |
| `WinLineEvent` | `WinLineInfo` | Highlight a winning way |
| `TumbleClearEvent` | — | Fade highlights between cascade steps |
| `SpinEndEvent` | `SpinResult` | Spin fully finished |
| `FreeSpinEvent` | `remainingFreeSpins`, `isLastSpin` | Chain the next free spin |
| `EnterFreeSpinEvent` / `ExitFreeSpinEvent` | — | Mode transitions (from `SlotModeEventDispatcher`) |
| `RespinEvent`, `EnterRespinEvent` / `ExitRespinEvent` | — | Respin plumbing (not triggered by the current game) |
| `WildMeterEvent`, `JackpotUpgradeEvent`, `UltraNudgeEvent` | — | **Unused stubs** (see Known issues) |

`SlotUnityEventRouter` re-exposes hub events as inspector-wired **UnityEvents with delays** so designers can hook audio, VFX or UI without code.

### Frontend (presentation)
- **`Reelmanager`** auto-discovers child `ReelView` / `ReelSpinAnimator`s, applies the `SlotLayoutConfig` and symbol sprite array, and runs per-reel coroutines in parallel for gravity drops and new-symbol drop-ins.
- **`ReelSpinAnimator`** scrolls symbols downward, wrapping them with random paying symbols (IDs 1–10, never blank/EMPTY), then snaps to the final result on `StopSpinning`.
- **`SymbolView`** listens for `WinLineEvent`, instantiates a glow prefab (scale-in), fades it out on `TumbleClearEvent`, and clears it instantly on `SpinStartEvent`. Cells are hidden (not re-sprited) while EMPTY during a cascade.
- All easing is hand-rolled (`EaseOutBounce`, `EaseOutBack`) — **no tweening library** is used.
- **`SlotUIManager`** is a passive sink for balance, win, free-spin counter, feature labels and the live-stats panel. Credits → dollars uses `creditsPerDollar` (20, matching `<Bet value>`).

---

## ⚙️ Configuration (XML)

All math lives in `Assets/Scripts/SlotConfig.XML`, assigned to `Server.configFile` in the scene.

| Section | Contents |
|---|---|
| `<Bet value="20"/>` | Base bet in credits; the paytable is calibrated to it |
| `<Paytable>` | One `<Symbol id name payout="…">` per symbol; payout array index = reels matched − 1 |
| `<ReelSets>` | `BaseGame0`, `BaseGame1`, `FreeGame0`; each has six `<Reel>` strips of quoted symbol names |
| `<Scatter_settings>` | `Spins`, `AdditionalSpins` and `ScatterPay`, each indexed by scatter count (0–6) |
| `<ReelSet_Selection>` | `baseReelSelection` — weighted choice of base reel set; `freeReelSelection` — reel set(s) used in free spins |

Every weighted table uses the same shape — `values="a,b,c" weights="x,y,z"` — and is read by `Server.GenerateWeightedRandomNumber`. Symbol names in reel strips must match `Server.SymbolNames` exactly; unknown names are logged as errors at parse time.

---

## 📊 Simulation & live stats

**Headless simulation:** tick `Server.simulationMode` and set `sampleSize` on the `Server` object, press Play, and the Console prints a report at base bet: total RTP split into Base Game / Free Spins (with scatter pay shown separately), win hit rates, free-spin sessions, retrigger rate, average and max free-spin session win, cascade depth stats, and peak single-spin win in credits and ×bet.

**Live stats:** the same metrics are computed cumulatively on every real spin and shown on the stats panel of `Proto_6x4` via `SlotUIManager.LiveStats()`. Press **Reset** to zero them.

---

## 🛠 Editor tools

| Tool | Where | What it does |
|---|---|---|
| **Reel Set Data** | `Create ▸ Slot ▸ Reel Set Data`, then inspector button **Parse and Transpose Reels** | Paste a tab-separated spreadsheet (rows = positions, columns = reels); outputs ready-to-paste `<Reel>"A","B",…</Reel>` XML blocks. `Assets/ReelsParsing.asset` is an empty scratchpad instance. |
| **Multi Payline Editor** | Add `MultiPaylineEditorTool` component to any GameObject | Toggle-grid designer for paylines (up to 10×10, 125 lines). **Generate Paylines** outputs a `~`-separated flat-index string in the format `LinesUtil.lineRules` expects. |
| **Sprite Extractor** | `Tools ▸ Extract Sprites From Atlas` | Select a sliced, *readable* `Texture2D`; writes each sub-sprite to `Assets/ExtractedSprites/` as PNG. |
| **Slot Layout Config** | `Create ▸ Slot ▸ SlotLayoutConfig` | Defines reels × rows; assign to `Reelmanager.layoutConfig`. |
| `[ReadOnly]` attribute | code | Greys out auto-wired inspector fields. |

---

## ➕ Adapting the prototype

1. **Math:** edit `SlotConfig.XML` — paytable, reel strips (use the Reel Set Data tool), scatter settings, reel-set weights. Keep `Server.SymbolNames` in sync if you rename or add symbols.
2. **Layout:** pick or create a `SlotLayoutConfig` (5×3, 6×4, 6×5, 6×8 provided). Set `Server.numberOfReels` / `slotHeight` to match, and build the matching number of `Reel_N` objects with `Slot_N` children under `ReelManager`.
3. **Art:** drop symbol sprites into `Reelmanager.symbolSprites` in symbol-ID order (index 11 is the EMPTY placeholder).
4. **Win mode:** `Server.InitGame()` instantiates `WaysUtil`; swap in `LinesUtil` (and update `lineRules` with the Payline Editor output) for a payline game.
5. **Tune feel:** adjust delays on `SpinResultVisualizer`, `Reelmanager` and `ReelSpinAnimator`; verify RTP with `simulationMode`.

---

## ⚠️ Known issues & legacy code

- `SlotUnityEventRouter.OnDisable` unsubscribes with fresh lambdas, so handlers are never actually removed (leak / double-fire on re-enable).
- `WildMeterEvent`, `JackpotUpgradeEvent` and `UltraNudgeEvent` are defined but never raised — safe to delete.
- Respin plumbing (`RespinEvent`, `SlotModeType.Respin`, `SlotModeEventDispatcher`) exists but nothing triggers it in the current game.
- `Config.XML` / `GameScene.unity` are the older 5×3 setup and are not part of the build.
- `SpinSpeedController` comments mention 1×–8× but only `{1, 2, 4}` are populated; `ReelSpinAnimator` does not scale its spin speed with it.
- `LinesUtil.lineRules` is a hard-coded 5×3, 20-line table; it is only used by the Ways path for pricing and would need regenerating (Payline Editor) before running a payline game on the 6×4 grid.

---

## 🧰 Tech stack

- **Unity 6000.3.15f1**, Universal Render Pipeline 17.3.0, 2D feature set
- **TextMeshPro** (UI text), **uGUI** 2.0, **Input System** 1.19
- Unity Animation clips/controllers for glow effects; hand-written coroutine easing for reels
- `System.Xml` for config parsing; `UnityEngine.Random` for RNG
- Solution/project files (`.sln`, `.csproj`) are generated by Unity and git-ignored
