# Changelog

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
