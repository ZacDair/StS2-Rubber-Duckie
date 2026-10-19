# Changelog

All notable changes to RubberDuckie will be documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

\---

## [0.1.1] — 2026-10-19

### Fixed

* Hits absorbed by Osty no longer squeak just because the player takes no damage
* Fully blocked hits stay silent when Osty is present
* Truly zero-damage hits with Osty present squeak once instead of once per recipient

### Changed

* Added automated combat regression tests using the game's damage implementation
* Added a release script that tests, exports with Megadot, checks packaged audio playback, and validates the ZIP and version metadata
* Release archives now contain the standard `RubberDuckie` install folder
* Removed unused template build dependencies and restricted the resource pack to the mod's assets

\---

## \[0.1.0] — 2026-04-11

### Added

* Squeak playback for qualifying 0-damage attack results
* Excluded ordinary fully blocked hits from triggering the squeak
* Slight random pitch variation for audio polish
* Initial public release

