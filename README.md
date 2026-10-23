# Power Plant Calculator

An in-game calculator for every fuel-burning power plant, GenesisBook plants included. Pick a plant and a fuel, then either enter how much fuel you make per minute or how much power you want. The calculator shows how many plants you need, how much fuel they burn and how much power you get.

Press **Left Alt + X** to open or close the window.

## How to use

1. **Pick a power plant.** The top of the window lists every fuel-burning plant, each with small icons of the fuels it accepts.
2. **Pick a fuel.** Clicking a plant opens its fuel menu. Hover an icon to see the game's item tooltip.
3. **Choose a direction:**
   - **Fuel → Power:** enter the fuel you produce per minute.
   - **Power → Fuel:** enter the power you want and pick its unit (W, kW, MW, GW, TW, PW).

   There is one input box. The selected direction decides how it is read, so fuel and power can never be entered at the same time. Each direction remembers its own last value.
4. **Optionally pick a proliferator level:** none, Mk.I, Mk.II or Mk.III.

## Results

| Line | Meaning |
| --- | --- |
| Use per plant (full load) | Fuel items one plant burns per minute at full output |
| Power per plant | Output of one plant at full load |
| Plants needed | Rounded up, with the exact value in brackets |
| Total fuel | Fuel items per minute for all plants |
| Total power | Combined output of all plants |

## Filters

Both filters are off by default, so the list shows every plant and every fuel.

- **Only this planet's focus:** shows only the plants boosted by the GenesisBook planet focus of the planet you are on (Fuel Cell +20%, Fission +20%, Artificial Star +10%) and includes that bonus in the result.
- **Only makeable on this planet:** scans the planet's veins, ocean and atmosphere/gas resources, follows recipe chains from them and keeps only the fuels you can make without leaving the planet. The ray receiver's critical photons count as available. The filter does not check whether the buildings for those recipes are unlocked.

## How the numbers are calculated

The calculator runs the game's own power plant code (`PowerGeneratorComponent.EnergyCap_Fuel`) and its fuel drain formula. Proliferator bonuses and the antimatter-rod doubling are therefore included, and so are mod changes to that code, such as GenesisBook's per-fuel reactor bonus.

Fuel-burning plants produce no by-products; the whole fuel turns into power. GenesisBook's books and developer logs can be burned, but they are collectibles and are not listed.

## Settings

In `BepInEx/config/bug.dsp.powerplantcalc.cfg`, or in your mod manager's config editor. Changes apply after restarting the game.

- `OpenWindow`: hotkey, default `X + LeftAlt`
- `Language`: `English` (default), `Chinese` or `Turkish`

## Compatibility

- **Works without GenesisBook.** The planet-focus filter then reports that GenesisBook is missing.
- **Makes no changes to the game.** It does not patch game code or touch save data.

## Building from source

You need a current .NET SDK and the .NET Framework 4.7.2 targeting pack. The game, Unity and BepInEx assemblies are referenced from your own installation and are not part of this repository.

```text
dotnet build -c Release "-p:DSPGamePath=D:\Games\Dyson Sphere Program" "-p:BepInExCorePath=C:\path\to\BepInEx\core"
```

The output is `bin/Release/net472/PowerPlantCalc.dll`. Copy it to `BepInEx/plugins/PowerPlantCalc/`.

## License

MIT, see [LICENSE](LICENSE).
