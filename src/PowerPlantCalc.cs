using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace PowerPlantCalc
{
    public enum Language { English, Chinese, Turkish }

    [BepInPlugin("bug.dsp.powerplantcalc", "PowerPlantCalc", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<KeyboardShortcut> hotkey;
        public static ConfigEntry<Language> language;

        private void Awake()
        {
            hotkey = Config.Bind("General", "OpenWindow", new KeyboardShortcut(KeyCode.X, KeyCode.LeftAlt), "Opens/closes the power plant calculator.");
            language = Config.Bind("General", "Language", Language.English, "Window language: English, Chinese or Turkish (applies after restarting the game).");
        }

        private void Update()
        {
            if (DSPGame.IsMenuDemo || !GameMain.isRunning || UIRoot.instance?.uiGame?.generatorWindow == null) return;
            if (!VFInput.inputing && hotkey.Value.IsDown()) CalcWindow.Toggle();
        }
    }

    public static class Lang
    {
        // English, Chinese, Turkish. Item and building names come from the game's own localisation.
        private static readonly Dictionary<string, string[]> texts = new Dictionary<string, string[]>
        {
            ["title"] = new[] { "Power Plant Calculator", "发电设施计算器", "Santral Hesaplayıcı" },
            ["onlyFocus"] = new[] { "Only this planet's focus", "仅本星球聚焦", "Sadece bu gezegenin odağı" },
            ["onlyPlanet"] = new[] { "Only makeable on this planet", "仅本星球可生产", "Sadece bu gezegende üretilebilenler" },
            ["plants"] = new[] { "Fuel-burning power plants", "燃料发电设施", "Yakıtla çalışan santraller" },
            ["modeFuel"] = new[] { "Fuel → Power", "燃料 → 电力", "Yakıt → Güç" },
            ["modePower"] = new[] { "Power → Fuel", "电力 → 燃料", "Güç → Yakıt" },
            ["amount"] = new[] { "Amount", "数量", "Miktar" },
            ["power"] = new[] { "Power", "功率", "Güç" },
            ["perMin"] = new[] { "/ min", "/ 分钟", "/ dk" },
            ["proliferator"] = new[] { "Proliferator", "增产剂", "Artırıcı" },
            ["noProliferator"] = new[] { "No proliferator", "无增产剂", "Artırıcı yok" },
            ["fuelOf"] = new[] { "{0} — fuel: {1}", "{0} — 燃料：{1}", "{0} — girdi kaynağı: {1}" },
            ["focusIncluded"] = new[] { "   (planet focus +{0}% included)", "   （含星球聚焦 +{0}%）", "   (gezegen odağı +%{0} dahil)" },
            ["usePerPlant"] = new[] { "Use per plant (full load)", "单台消耗（满载）", "Santral başına tüketim (tam yük)" },
            ["powerPerPlant"] = new[] { "Power per plant", "单台功率", "Santral başına güç" },
            ["plantsNeeded"] = new[] { "Plants needed", "所需设施数量", "Gereken santral" },
            ["totalFuel"] = new[] { "Total fuel", "燃料总量", "Toplam yakıt" },
            ["totalPower"] = new[] { "Total power", "总功率", "Toplam güç" },
            ["noPlanet"] = new[] { "Filters need you to be on a planet.", "筛选需要位于星球上。", "Filtreler için bir gezegende olmalısın." },
            ["noGenesis"] = new[] { "Planet focus comes from GenesisBook; GenesisBook was not found.", "星球聚焦来自创世之书；未找到创世之书。", "Gezegen odağı GenesisBook ile gelir; GenesisBook bulunamadı." },
            ["noFocus"] = new[] { "This planet's focus boosts no power plant.", "本星球的聚焦未加成任何发电设施。", "Bu gezegenin odağında bir santral bonusu yok." },
            ["noFocusOrMake"] = new[] { "This planet's focus boosts no power plant, or its fuel can't be made here.", "本星球的聚焦未加成任何发电设施，或其燃料无法在此生产。", "Bu gezegenin odağında bir santral bonusu yok ya da yakıtı burada üretilemiyor." },
            ["noMake"] = new[] { "No fuel can be made on this planet.", "本星球无法生产任何燃料。", "Bu gezegende üretilebilen bir yakıt bulunamadı." },
        };

        public static string T(string key) => texts[key][(int)(Plugin.language?.Value ?? Language.English)];
    }

    public struct Result
    {
        public double PerMachine;  // fuel items per minute for one plant at full load
        public double MachinePower; // W
        public double Machines;
        public double TotalPower;   // W
    }

    public static class Calc
    {
        public static readonly int[] IncLevels = { 0, 1, 2, 4 }; // none, Mk.I, Mk.II, Mk.III
        public static readonly string[] Units = { "W", "kW", "MW", "GW", "TW", "PW" };

        // Runs the game's own EnergyCap_Fuel (so mod patches such as GenesisBook's reactor bonus apply) and the GenEnergyByFuel drain.
        // focusMultiplier mirrors GenesisBook's planet focus, which scales capacity in PowerSystem.GameTick, outside EnergyCap_Fuel.
        public static Result Run(long genEnergyPerTick, long useFuelPerTick, int fuelMask, int fuelId, long heatValue, bool productive, int incLevel, double ratePerMinute, double focusMultiplier = 1)
        {
            var g = new PowerGeneratorComponent
            {
                genEnergyPerTick = genEnergyPerTick, useFuelPerTick = useFuelPerTick, fuelMask = (short)fuelMask,
                fuelId = (short)fuelId, curFuelId = (short)fuelId, fuelCount = 1, productive = productive, fuelIncLevel = (byte)incLevel,
            };
            long cap = (long)(g.EnergyCap_Fuel() * focusMultiplier);
            if (cap <= 0 || heatValue <= 0 || genEnergyPerTick <= 0) return default;
            double drain = productive
                ? (double)cap * useFuelPerTick * 40 / ((double)genEnergyPerTick * Cargo.incFastDivisionNumerator[incLevel])
                : (double)cap * useFuelPerTick / genEnergyPerTick;
            var r = new Result { PerMachine = drain * 3600 / heatValue, MachinePower = cap * 60.0 };
            r.Machines = ratePerMinute / r.PerMachine;
            r.TotalPower = r.Machines * r.MachinePower;
            return r;
        }

        // Fuel per minute that yields `watts`, from any Run result of the same plant/fuel/proliferator.
        public static double RateForPower(Result r, double watts) => r.MachinePower > 0 ? watts / r.MachinePower * r.PerMachine : 0;

        // Everything reachable from `have` by repeatedly applying recipes whose inputs are all available.
        public static HashSet<int> Closure(HashSet<int> have, IList<(int[] items, int[] results)> recipes)
        {
            for (bool grew = true; grew;)
            {
                grew = false;
                foreach (var (items, results) in recipes)
                    if (items.All(have.Contains))
                        foreach (var r in results) grew |= have.Add(r);
            }
            return have;
        }

        public static string Power(double watts)
        {
            int u = 0;
            while (Math.Abs(watts) >= 1000 && u < Units.Length - 1) { watts /= 1000; u++; }
            return watts.ToString("0.##") + " " + Units[u];
        }
    }

    // Current planet's state: GenesisBook planet focus and what can be made from local resources.
    public static class Planet
    {
        private static Func<int, int, bool> containsFocus;
        private static bool focusLooked;

        public static bool HasFocusSystem => Focus() != null;

        private static Func<int, int, bool> Focus()
        {
            if (focusLooked) return containsFocus;
            focusLooked = true;
            var m = AccessTools.Method("ProjectGenesis.Patches.PlanetFocusPatches:ContainsFocus");
            if (m != null) containsFocus = (Func<int, int, bool>)Delegate.CreateDelegate(typeof(Func<int, int, bool>), m);
            return containsFocus;
        }

        // Same table as GenesisBook's PlanetFocusPatches.EnergyCap_Patch: fuel mask -> (focus id, capacity multiplier).
        public static double FocusMultiplier(PlanetData p, int fuelMask)
        {
            int id; double mult;
            switch (fuelMask)
            {
                case 1: id = 6524; mult = 1.2; break;
                case 2: id = 6529; mult = 1.2; break;
                case 16: id = 6527; mult = 1.1; break;
                default: return 1;
            }
            return p != null && Focus() != null && Focus()(p.id, id) ? mult : 1;
        }

        public static HashSet<int> Producible(PlanetData p)
        {
            var have = new HashSet<int>();
            foreach (var vg in p.veinGroups ?? new VeinGroup[0])
            {
                var vein = vg.isNull || vg.amount <= 0 ? null : LDB.veins.Select((int)vg.type);
                if (vein != null && vein.MiningItem > 0) have.Add(vein.MiningItem);
            }
            if (p.waterItemId > 0) have.Add(p.waterItemId);
            foreach (var gas in p.gasItems ?? new int[0]) if (gas > 0) have.Add(gas);
            // Ray receivers in photon mode make their product from starlight on any planet.
            foreach (var i in LDB.items.dataArray) if (i?.prefabDesc != null && i.prefabDesc.gammaRayReceiver && i.prefabDesc.powerProductId > 0) have.Add(i.prefabDesc.powerProductId);
            var recipes = LDB.recipes.dataArray.Where(r => r != null && r.GridIndex > 0 && r.Items != null && r.Results != null && r.Results.Length > 0).Select(r => (r.Items, r.Results)).ToList();
            return Calc.Closure(have, recipes);
        }
    }

    public static class CalcWindow
    {
        private const float Width = 620f, CardW = 150f, CardH = 132f, CardsTop = -100f;
        private static GameObject win, fuelTpl, switchTpl, buttonTpl;
        private static RectTransform root, cardsRoot, fuelSection, fuelGrid, modeRow, inputRow, results;
        private static Text labelTpl, valueTpl, fuelTitle, emptyText, amountLabel, perMinText;
        private static InputField amountInput;
        private static readonly List<Text> resultValues = new List<Text>();
        private static readonly List<(ItemProto item, Image ring)> genRings = new List<(ItemProto, Image)>(), fuelRings = new List<(ItemProto, Image)>();
        private static readonly List<Image> incRings = new List<Image>();
        private static readonly List<UIButton> modeButtons = new List<UIButton>(), unitButtons = new List<UIButton>();
        private static ItemProto gen, fuel;
        private static int incIndex, unitIndex = 3; // GW
        private static bool powerMode; // false: fuel per minute in, power out; true: power in, fuel per minute out
        private static readonly double[] amounts = { 60, 1 }; // last value typed in each mode
        private static float cardsBottom;
        private static bool onlyFocus, onlyPlanet;
        private static PlanetData planet;
        private static HashSet<int> producible;

        public static void Toggle()
        {
            if (win == null) Build();
            win.SetActive(!win.activeSelf);
            if (!win.activeSelf) return;
            win.transform.SetAsLastSibling();
            RebuildCards(); // the player may have moved to another planet
        }

        // Every fuel-burning plant, GenesisBook ones included; wind, solar, geothermal and ray receivers burn nothing.
        private static IEnumerable<ItemProto> Generators() => LDB.items.dataArray.Where(i => i?.prefabDesc != null && i.prefabDesc.isPowerGen && i.prefabDesc.fuelMask > 0 && i.prefabDesc.useFuelPerTick > 0 && i.prefabDesc.fuelMask < ItemProto.fuelNeeds.Length && i.GridIndex > 0);

        private static ItemProto[] Fuels(ItemProto g) => (ItemProto.fuelNeeds[g.prefabDesc.fuelMask] ?? new int[0])
            .Where(id => producible == null || producible.Contains(id)).Select(id => LDB.items.Select(id)).Where(i => i != null && !IsLoreItem(i)).ToArray();

        // GenesisBook's three books and developer logs burn in any plant but are collectibles, not fuel (matched by proto key, not translated name).
        private static bool IsLoreItem(ItemProto i) => i.Name == "创世之书" || i.Name == "虚空之书" || i.Name == "起源之书" || (i.Name?.StartsWith("开发者日志") ?? false);

        private static bool Visible(ItemProto g) => (!onlyFocus || Planet.FocusMultiplier(planet, g.prefabDesc.fuelMask) > 1) && Fuels(g).Length > 0;

        private static void Build()
        {
            var src = UIRoot.instance.uiGame.generatorWindow.transform;
            fuelTpl = src.Find("produce-2/fuel").gameObject;
            switchTpl = src.Find("boost-switch").gameObject;
            buttonTpl = src.Find("ray-receiver/switch-button-1").gameObject;
            labelTpl = src.Find("power-network-desc/desc-group/label-1").GetComponent<Text>();
            valueTpl = src.Find("power-network-desc/desc-group/value-1").GetComponent<Text>();

            // The generator window's frame (border, title, drag, close) with its contents and behaviour removed.
            win = UnityEngine.Object.Instantiate(src.gameObject, src.parent, false);
            win.name = "Power Plant Calculator";
            UnityEngine.Object.DestroyImmediate(win.GetComponent<UIPowerGeneratorWindow>());
            foreach (Transform c in win.transform) if (c.name != "panel-bg") UnityEngine.Object.Destroy(c.gameObject);
            UnityEngine.Object.Destroy(win.transform.Find("panel-bg/btn-box/menu-wnd-btn").gameObject);
            StripLocalizers(win);
            var close = win.transform.Find("panel-bg/btn-box/close-wnd-btn").GetComponent<Button>();
            close.onClick = new Button.ButtonClickedEvent();
            close.onClick.AddListener(() => win.SetActive(false));
            var title = win.transform.Find("panel-bg/title-text").GetComponent<Text>();
            title.text = Lang.T("title");
            title.rectTransform.sizeDelta = new Vector2(400f, title.rectTransform.sizeDelta.y);
            root = (RectTransform)win.transform;
            root.anchoredPosition = new Vector2(0f, 330f);

            Switch(new Vector2(20f, -50f), Lang.T("onlyFocus"), on => { onlyFocus = on; RebuildCards(); });
            Switch(new Vector2(320f, -50f), Lang.T("onlyPlanet"), on => { onlyPlanet = on; RebuildCards(); });
            NewText(labelTpl, root, new Vector2(20f, -76f), new Vector2(400f, 20f), TextAnchor.MiddleLeft, Lang.T("plants"));
            cardsRoot = NewRect("cards", root, new Vector2(0f, CardsTop), new Vector2(Width, 10f));
            emptyText = NewText(labelTpl, root, new Vector2(20f, CardsTop), new Vector2(580f, 40f), TextAnchor.UpperLeft, "");
            emptyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            fuelSection = NewRect("fuel-section", root, Vector2.zero, new Vector2(Width, 10f));
            fuelTitle = NewText(labelTpl, fuelSection, new Vector2(20f, 0f), new Vector2(580f, 20f), TextAnchor.MiddleLeft, "");
            fuelGrid = NewRect("fuel-grid", fuelSection, new Vector2(0f, -24f), new Vector2(Width, 10f));

            // Mode row: one input field, read either as fuel per minute or as power, never both.
            modeRow = NewRect("mode-row", fuelSection, Vector2.zero, new Vector2(Width, 44f));
            modeButtons.Add(TextButton(modeRow, new Vector2(20f, -8f), new Vector2(140f, 28f), Lang.T("modeFuel"), () => SetMode(false)));
            modeButtons.Add(TextButton(modeRow, new Vector2(166f, -8f), new Vector2(140f, 28f), Lang.T("modePower"), () => SetMode(true)));
            NewText(labelTpl, modeRow, new Vector2(330f, -12f), new Vector2(90f, 20f), TextAnchor.MiddleLeft, Lang.T("proliferator"));
            int[] incItems = { 0, 1141, 1142, 1143 };
            for (int i = 0; i < incItems.Length; i++)
            {
                int index = i;
                var ring = Circle(modeRow, new Vector2(420f + i * 46f, -3f), 38f, LDB.items.Select(incItems[i]), () => { incIndex = index; Refresh(); });
                if (i == 0) ring.transform.parent.Find("button").GetComponent<UIButton>().tips.tipTitle = Lang.T("noProliferator");
                incRings.Add(ring);
            }

            inputRow = NewRect("input-row", fuelSection, Vector2.zero, new Vector2(Width, 30f));
            amountLabel = NewText(labelTpl, inputRow, new Vector2(20f, -4f), new Vector2(70f, 20f), TextAnchor.MiddleLeft, "");
            amountInput = UnityEngine.Object.Instantiate(UIRoot.instance.uiGame.beltWindow.transform.Find("number-input").GetComponent<InputField>(), inputRow, false);
            StripLocalizers(amountInput.gameObject);
            Place((RectTransform)amountInput.transform, new Vector2(90f, -4f), new Vector2(80f, 22f));
            amountInput.onValueChanged = new InputField.OnChangeEvent();
            amountInput.onEndEdit = new InputField.EndEditEvent();
            amountInput.contentType = InputField.ContentType.DecimalNumber;
            amountInput.characterLimit = 10;
            amountInput.onValueChanged.AddListener(s =>
            {
                double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out amounts[powerMode ? 1 : 0]);
                Refresh();
            });
            perMinText = NewText(labelTpl, inputRow, new Vector2(176f, -4f), new Vector2(60f, 20f), TextAnchor.MiddleLeft, Lang.T("perMin"));
            for (int i = 0; i < Calc.Units.Length; i++)
            {
                int index = i;
                unitButtons.Add(TextButton(inputRow, new Vector2(176f + i * 46f, -3f), new Vector2(42f, 24f), Calc.Units[i], () => { unitIndex = index; Refresh(); }));
            }

            results = NewRect("results", fuelSection, Vector2.zero, new Vector2(Width, 130f));
            string[] labels = { "usePerPlant", "powerPerPlant", "plantsNeeded", "totalFuel", "totalPower" };
            for (int i = 0; i < labels.Length; i++)
            {
                NewText(labelTpl, results, new Vector2(20f, -i * 24f), new Vector2(330f, 20f), TextAnchor.MiddleLeft, Lang.T(labels[i]));
                resultValues.Add(NewText(valueTpl, results, new Vector2(Width - 290f, -i * 24f), new Vector2(270f, 20f), TextAnchor.MiddleRight, ""));
            }
            SetMode(false);
        }

        private static void SetMode(bool power)
        {
            powerMode = power;
            amountLabel.text = Lang.T(power ? "power" : "amount");
            perMinText.gameObject.SetActive(!power);
            foreach (var b in unitButtons) b.gameObject.SetActive(power);
            amountInput.text = amounts[power ? 1 : 0].ToString(CultureInfo.InvariantCulture); // fires onValueChanged, which refreshes
            Refresh();
        }

        // The generator window's on/off switch (sandbox boost), relabelled.
        private static void Switch(Vector2 pos, string label, Action<bool> onToggle)
        {
            var go = UnityEngine.Object.Instantiate(switchTpl, root, false);
            var label0 = go.transform.Find("label");
            if (label0 != null) UnityEngine.Object.Destroy(label0.gameObject);
            go.SetActive(true);
            var rt = (RectTransform)go.transform;
            Place(rt, pos, rt.sizeDelta);
            var sw = go.GetComponent<UISwitch>();
            sw.SetToggleNoEvent(false);
            sw.onToggle += onToggle;
            NewText(labelTpl, root, pos + new Vector2(46f, 2f), new Vector2(250f, 20f), TextAnchor.MiddleLeft, label);
        }

        // The ray receiver's mode button (power / photon), used as a radio button via UIButton.highlighted.
        private static UIButton TextButton(Transform parent, Vector2 pos, Vector2 size, string label, Action onClick)
        {
            var go = UnityEngine.Object.Instantiate(buttonTpl, parent, false);
            StripLocalizers(go);
            Place((RectTransform)go.transform, pos, size);
            go.GetComponentInChildren<Text>().text = label;
            var b = go.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(() => onClick());
            var ub = go.GetComponent<UIButton>();
            ub.tips.tipTitle = "";
            ub.tips.tipText = "";
            return ub;
        }

        private static void RebuildCards()
        {
            planet = GameMain.localPlanet;
            producible = onlyPlanet && planet != null ? Planet.Producible(planet) : null;
            foreach (Transform c in cardsRoot) UnityEngine.Object.Destroy(c.gameObject);
            genRings.Clear();
            bool noPlanet = (onlyFocus || onlyPlanet) && planet == null;
            var gens = noPlanet ? new ItemProto[0] : Generators().Where(Visible).ToArray();
            for (int i = 0; i < gens.Length; i++)
                BuildCard(gens[i], new Vector2(15f + i % 4 * (CardW + 2f), -i / 4 * CardH));
            cardsBottom = CardsTop - (gens.Length > 0 ? (gens.Length + 3) / 4 * CardH : 44f);
            emptyText.text = gens.Length > 0 ? ""
                : noPlanet ? Lang.T("noPlanet")
                : onlyFocus && !Planet.HasFocusSystem ? Lang.T("noGenesis")
                : onlyFocus ? Lang.T(onlyPlanet ? "noFocusOrMake" : "noFocus")
                : Lang.T("noMake");
            fuelSection.anchoredPosition = new Vector2(0f, cardsBottom);
            if (!gens.Contains(gen)) gen = gens.FirstOrDefault();
            fuelSection.gameObject.SetActive(gen != null);
            if (gen != null) SelectGen(gen);
            else root.sizeDelta = new Vector2(Width, -cardsBottom + 20f);
        }

        private static void BuildCard(ItemProto g, Vector2 pos)
        {
            var card = NewRect("card-" + g.ID, cardsRoot, pos, new Vector2(CardW, CardH));
            genRings.Add((g, Circle(card, new Vector2((CardW - 52f) / 2f, 0f), 52f, g, () => SelectGen(g))));
            NewText(labelTpl, card, new Vector2(0f, -54f), new Vector2(CardW, 34f), TextAnchor.UpperCenter, g.name).horizontalOverflow = HorizontalWrapMode.Wrap;
            // Small icons of what this plant burns; the full list opens in the fuel menu.
            var fuels = Fuels(g);
            const int perRow = 7, max = 14;
            int shown = fuels.Length > max ? max - 1 : fuels.Length;
            for (int i = 0; i < shown; i++)
            {
                var img = new GameObject("fuel-" + fuels[i].ID, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                img.transform.SetParent(card, false);
                img.sprite = fuels[i].iconSprite;
                img.raycastTarget = false;
                Place(img.rectTransform, new Vector2(6f + i % perRow * 20f, -90f - i / perRow * 20f), new Vector2(18f, 18f));
            }
            if (shown < fuels.Length)
                NewText(labelTpl, card, new Vector2(6f + shown % perRow * 20f, -90f - shown / perRow * 20f), new Vector2(30f, 18f), TextAnchor.MiddleLeft, "+" + (fuels.Length - shown));
        }

        private static void SelectGen(ItemProto g)
        {
            gen = g;
            foreach (Transform c in fuelGrid) UnityEngine.Object.Destroy(c.gameObject);
            fuelRings.Clear();
            var fuels = Fuels(g);
            const int perRow = 13;
            for (int i = 0; i < fuels.Length; i++)
            {
                var f = fuels[i];
                fuelRings.Add((f, Circle(fuelGrid, new Vector2(15f + i % perRow * 45f, -i / perRow * 45f), 42f, f, () => { fuel = f; Refresh(); })));
            }
            if (!fuels.Contains(fuel)) fuel = fuels.FirstOrDefault();
            float y = -30f - (fuels.Length + perRow - 1) / perRow * 45f;
            modeRow.anchoredPosition = new Vector2(0f, y);
            inputRow.anchoredPosition = new Vector2(0f, y - 48f);
            results.anchoredPosition = new Vector2(0f, y - 48f - 36f);
            root.sizeDelta = new Vector2(Width, -cardsBottom - y + 48f + 36f + 5 * 24f + 30f);
            Refresh();
        }

        private static void Refresh()
        {
            foreach (var (item, ring) in genRings) ring.fillAmount = item == gen ? 1f : 0f;
            foreach (var (item, ring) in fuelRings) ring.fillAmount = item == fuel ? 1f : 0f;
            for (int i = 0; i < incRings.Count; i++) incRings[i].fillAmount = i == incIndex ? 1f : 0f;
            for (int i = 0; i < modeButtons.Count; i++) modeButtons[i].highlighted = (i == 1) == powerMode;
            for (int i = 0; i < unitButtons.Count; i++) unitButtons[i].highlighted = i == unitIndex;
            if (gen == null) return;
            double focus = onlyFocus ? Planet.FocusMultiplier(planet, gen.prefabDesc.fuelMask) : 1;
            fuelTitle.text = string.Format(Lang.T("fuelOf"), gen.name, fuel?.name ?? "") + (focus > 1 ? string.Format(Lang.T("focusIncluded"), Math.Round((focus - 1) * 100)) : "");
            double amount = amounts[powerMode ? 1 : 0];
            if (fuel == null || amount <= 0) { Clear(); return; }
            var pd = gen.prefabDesc;
            Result Run(double rate) => Calc.Run(pd.genEnergyPerTick, pd.useFuelPerTick, pd.fuelMask, fuel.ID, fuel.HeatValue, fuel.Productive, Calc.IncLevels[incIndex], rate, focus);
            double fuelRate = powerMode ? Calc.RateForPower(Run(1), amount * Math.Pow(1000, unitIndex)) : amount;
            var r = Run(fuelRate);
            if (r.PerMachine <= 0) { Clear(); return; }
            resultValues[0].text = r.PerMachine.ToString("0.##") + " " + Lang.T("perMin");
            resultValues[1].text = Calc.Power(r.MachinePower);
            resultValues[2].text = Math.Ceiling(r.Machines - 1e-9) + "   (" + r.Machines.ToString("0.##") + ")";
            resultValues[3].text = fuelRate.ToString("0.##") + " " + Lang.T("perMin");
            resultValues[4].text = Calc.Power(r.TotalPower);
        }

        private static void Clear()
        {
            foreach (var v in resultValues) v.text = "—";
        }

        // The generator window's fuel slot: ring (selection), icon and the game's item tooltip.
        private static Image Circle(Transform parent, Vector2 pos, float size, ItemProto item, Action onClick)
        {
            var go = UnityEngine.Object.Instantiate(fuelTpl, parent, false);
            // Keep only what we use; anything else (the game's count/arrows, or text other mods add to this slot) would be cloned too.
            foreach (Transform t in go.transform)
                if (t.name != "circle-fg" && t.name != "fuel-icon" && t.name != "button") UnityEngine.Object.Destroy(t.gameObject);
            foreach (Transform t in go.transform.Find("fuel-icon")) UnityEngine.Object.Destroy(t.gameObject);
            StripLocalizers(go);
            Place((RectTransform)go.transform, pos, new Vector2(size, size));
            var icon = go.transform.Find("fuel-icon").GetComponent<Image>();
            icon.sprite = item?.iconSprite;
            icon.enabled = icon.sprite != null;
            icon.rectTransform.sizeDelta = new Vector2(size * 0.62f, size * 0.62f);
            var ring = go.transform.Find("circle-fg").GetComponent<Image>();
            ring.transform.localEulerAngles = Vector3.zero;
            var button = go.transform.Find("button");
            var b = button.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(() => onClick());
            var ub = button.GetComponent<UIButton>();
            if (ub != null)
            {
                ub.tips.itemId = item?.ID ?? 0;
                ub.tips.itemCount = 0;
                ub.tips.itemInc = 0;
                ub.tips.type = UIButton.ItemTipType.Item;
                ub.tips.tipTitle = item?.name ?? "";
                ub.tips.tipText = "";
            }
            return ring;
        }

        private static Text NewText(Text tpl, Transform parent, Vector2 pos, Vector2 size, TextAnchor align, string s)
        {
            var t = UnityEngine.Object.Instantiate(tpl, parent, false);
            StripLocalizers(t.gameObject);
            Place(t.rectTransform, pos, size);
            t.alignment = align;
            t.text = s;
            t.raycastTarget = false;
            return t;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            Place(rt, pos, size);
            return rt;
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        // Cloned game texts would otherwise be reset to their original translation keys.
        private static void StripLocalizers(GameObject go)
        {
            foreach (var l in go.GetComponentsInChildren<Localizer>(true)) UnityEngine.Object.DestroyImmediate(l);
        }
    }
}
