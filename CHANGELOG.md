# Changelog

## 1.2.3

- The close button sits in a box of its own size: the window no longer shows an empty slot left of the X.

## 1.2.2

- With GenesisBook, items the game hides are no longer offered: Mk.I and Mk.II proliferators are gone from the proliferator choice and from the thermal plant's fuels, and so is the crystal explosive unit.

## 1.2.1

- Fixed values and headings glowing in the new look.

## 1.2.0

- New look built from the game's own window colours and fonts: a deeper panel that stays readable on bright ground, glass buttons, a visible amount field that highlights while you type.
- Section titles, the selected plant's name and the totals stand out. The figure the current mode answers is larger.
- Margins line up on both sides, and the small fuel icons are centred under each plant.

## 1.1.0

- Redesigned look: dark buttons with a warm accent, restyled switches, grouped results.

## 1.0.3

- The planet filters now follow you to a new planet while the window stays open.
- With GenesisBook, the planet filters now list only fuel cells for thermal plants on planets without oxygen, as the game does.

## 1.0.2

- Fixed overlapping text in the calculator window when GeneratorFuelRate (or another mod that adds text to the power plant window) is installed.

## 1.0.1

- Added a link to the GitHub source repository.

## 1.0.0

- First release.
- Calculator window opened and closed with Left Alt + X, built from the game's own UI parts.
- Lists every fuel-burning power plant, GenesisBook plants included, with icons of the fuels each one accepts.
- Two directions:
  - **Fuel → Power:** enter fuel per minute.
  - **Power → Fuel:** enter the power you want, with a unit from W to PW.
  - Only one input is active at a time.
- Proliferator level selection: none, Mk.I, Mk.II, Mk.III.
- Results:
  - fuel use per plant at full load
  - power per plant
  - plants needed
  - total fuel
  - total power
- Calculation runs the game's own power plant code, so mod bonuses such as GenesisBook's reactor bonus are included.
- Filters:
  - **Only this planet's focus:** includes the GenesisBook focus bonus.
  - **Only makeable on this planet:** follows recipe chains from the planet's own resources.
- GenesisBook books and developer logs are not listed as fuel.
- English (default), Chinese and Turkish.
