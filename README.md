# RubberDuckie

A small Slay the Spire 2 audio mod that plays a rubber duck squeak when an attack resolves to **0 damage** for reasons other than ordinary block.

The goal is simple: truly harmless attacks should sound ridiculous.

\---

## What it does

RubberDuckie plays a squeak when an attack ends up doing **no actual damage** because its damage was reduced to zero or otherwise nullified before a normal blocked-hit outcome.

Examples of the intended behaviour include cases like:

* Attack damage reduced to 0 by debuffs or stat loss
* Attacks that effectively become harmless due to special combat rules
* Other resolved attack outcomes where the final damage is 0 without being a standard blocked hit

RubberDuckie also adds slight pitch variation so repeated squeaks sound a bit more lively.

\---

## What it does **not** do

RubberDuckie is intentionally **not** meant to squeak when an otherwise normal attack is simply absorbed by block.

If an enemy attacks for damage and that damage is prevented by regular block as expected, that should remain silent.
Hits absorbed by Osty also stay silent: the attack still dealt damage, even though the player lost no HP.

\---

## Features

* Plays a squeak on qualifying 0-damage attack results
* Does **not** trigger on ordinary fully blocked hits
* Does **not** trigger when Osty absorbs a damaging hit
* Slight random pitch variation for better audio feel
* Lightweight and focused
* No gameplay balance changes

\---

## Install

### Manual install

When upgrading, remove the previous RubberDuckie folder first. The original v0.1.0 archive used the folder name `RubberDuckie-v0.1.0`; leaving it installed alongside the new folder creates a duplicate mod.

1. Close Slay the Spire 2
2. Open your Slay the Spire 2 install directory
3. Open or create the `mods` folder
4. Extract the included `RubberDuckie` folder into `mods`
5. Confirm the final structure looks like this:

```
Slay the Spire 2/
  mods/
    RubberDuckie/
      RubberDuckie.dll
      RubberDuckie.pck
      RubberDuckie.json
```

6. Launch the game
7. If prompted, relaunch in modded mode

### Vortex

Install the release archive through Vortex as normal.

After installation, verify the mod ended up in the game's `mods/RubberDuckie` folder and that the folder contains:

* `RubberDuckie.dll`
* `RubberDuckie.pck`
* `RubberDuckie.json`

\---

## Usage

There is no configuration menu and no setup required.

Install the mod, launch the game, and play normally. If a qualifying attack resolves to 0 damage, the squeak will play automatically.

\---

## Compatibility

* Intended as a lightweight audio / quality-of-life mod
* Does not intentionally change combat logic or balance
* Should be low-risk to use alongside many other mods, though conflicts are always possible if another mod patches the same combat or audio flow
* Verified in live combat on Steam **v0.107.1**, the latest main-branch release as of **2026-10-19**; later beta builds have not been tested
* Live checks confirmed that Osty interception and ordinary block stay silent, while a genuinely zero-damage attack plays one squeak, including during a normal enemy turn
* All 18 automated combat regressions pass using the same game version, Megadot **4.5.1.m.9**, and **.NET 9**; the exported sound pack also passes a playback check

If another mod also alters attack-result handling or impact audio behaviour, load-order or patch interaction issues may occur.
---

### Current behaviour notes

RubberDuckie targets resolved attack outcomes where:

* Final unblocked damage and overkill damage are `0` for both the target and any pet absorbing the hit
* No regular block damage was consumed by either recipient
* Neither recipient's result is treated as a standard fully blocked hit

Each resolved hit squeaks at most once per target and its pets. Separate targets in an area attack and successive hits are evaluated independently.

That distinction is deliberate — the mod is meant to react to attacks that become harmless in an unusual way, not to routine defensive block.

\---

## Known limitations

* Edge-case combat interactions may encode "0 damage" differently than expected
* Some unusual enemy or modded interactions may need follow-up tuning
* If another mod replaces the same combat hook behaviour, this mod may not trigger as intended

If you find a case that clearly should squeak but does not, or one that squeaks when it should not, please report it.

\---

## Bug reports

If you open an issue, please include:

* Game version
* Mod version
* Install method (manual or Vortex)
* What happened
* What you expected to happen
* Exact reproduction steps
* Whether other mods were enabled

Helpful extras: a screenshot, short clip, log output, or the enemy / card / effect involved.

\---

## Building from source

On Windows, install the .NET 9 SDK and use Megadot 4.5.1 with C# support. The script uses `Directory.Build.props` for the Megadot path and discovers the installed game automatically. Paths can also be supplied with `-GodotPath` and `-Sts2Path`.

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-release.ps1
```

From WSL, use the installed Windows tools:

```bash
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(wslpath -w scripts/build-release.ps1)"
```

The script builds the release DLL, runs combat regressions in an isolated headless Megadot test project, exports the resource pack, loads and plays the packaged squeak, and checks the archive layout and version metadata. It ensures the packaged DLL is the same DLL used by the combat tests. The test engine uses .NET 9 even if a newer runtime is installed, matching the game's Harmony support.

Outputs are written to `artifacts/`: the versioned ZIP, its SHA-256 checksum, and the combat test log. The ZIP contains only:

```
RubberDuckie/
  RubberDuckie.dll
  RubberDuckie.pck
  RubberDuckie.json
```

Do not ship source files, `bin`, `obj`, or editor metadata in the end-user release archive.

\---

## Credits

Created by Zac Dair.

\---

## License

MIT — see [LICENSE](LICENSE) for details.

\---

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

