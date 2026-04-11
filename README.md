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

\---

## Features

* Plays a squeak on qualifying 0-damage attack results
* Does **not** trigger on ordinary fully blocked hits
* Slight random pitch variation for better audio feel
* Lightweight and focused
* No gameplay balance changes

\---

## Install

### Manual install

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
* Tested on StS2 Steam version: v0.103.0

If another mod also alters attack-result handling or impact audio behaviour, load-order or patch interaction issues may occur.
---

### Current behaviour notes

RubberDuckie targets resolved attack outcomes where:

* Final unblocked damage is `0`
* No regular block damage was consumed
* The event is not treated as a standard fully blocked hit

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

Clone the project, open it in Rider or your preferred C# / Godot workflow, and build the mod. Then package the release output into the standard installable folder structure:

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

