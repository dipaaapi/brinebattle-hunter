using System.Linq;
using BrineBattle.Companions;
using BrineBattle.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Pets menu (Ed's ask: pet management is its own menu, not part of the Bag). Press P or the HUD
    /// button: roles, active Familiar, Crew/Mystic chores, Feed, Fuse (Transmutation Circle), looks, release.
    /// </summary>
    public class PetsPanel : MonoBehaviour
    {
        public HookController hook;
        static bool open => WorldPause.Open == Menu.Pets;
        Vector2 scroll;

        public static void Toggle() => WorldPause.Toggle(Menu.Pets);

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.pKey.wasPressedThisFrame && !BattleController.InBattle
                && (WorldPause.Open == Menu.None || WorldPause.Open == Menu.Pets)) Toggle();
        }

        static readonly string[] PageLabels = { "My pets", "Chores" };
        int page;
        Companion picked;
        Vector2 listScroll;

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!open || hook == null) return;
            GUI.depth = -5;
            float w = Mathf.Min(640, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(560, HuntStyles.H - 120);
            var roster = hook.Companions;
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), $"Pets ({roster.All.Count}/{roster.Capacity})", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Pets); GUILayout.EndArea(); return; }
            // Owner idea U-25: pick a pet on the left, everything about it on the right.
            page = HuntStyles.Tabs(page, PageLabels);
            if (page == 1) { scroll = GUILayout.BeginScrollView(scroll); DrawChores(); GUILayout.EndScrollView(); GUILayout.EndArea(); return; }

            if (roster.All.Count == 0)
            {
                GUILayout.Label("No pets yet.", HuntStyles.Heading);
                GUILayout.Label("Weaken a calm monster in battle (\"Twitching...\"), then press Tame. Build a habitat on the raft for more room.", HuntStyles.Text);
                GUILayout.EndArea();
                return;
            }
            if (picked == null || !roster.All.Contains(picked)) picked = roster.All[0];

            GUILayout.BeginHorizontal();
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Width(190));
            foreach (var c in roster.All)
            {
                var old = GUI.backgroundColor;
                if (c == picked) GUI.backgroundColor = HuntStyles.Go;
                string battle = roster.Active == c ? " ⚔" : "";
                if (GUILayout.Button($"<color={BagPanel.RarityColor(c.Rarity)}><b>{c.Name}</b></color>{battle}\n<size=11>{c.Role} · {c.StageName}</size>", HuntStyles.RichButton, GUILayout.Height(44)))
                { picked = c; feeding = fusing = null; releasing = null; }
                GUI.backgroundColor = old;
            }
            GUILayout.Label($"Room: {roster.All.Count}/{roster.Capacity}", HuntStyles.Hint);
            GUILayout.EndScrollView();
            scroll = GUILayout.BeginScrollView(scroll);
            Detail(picked);
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void Detail(Companion c)
        {
            var roster = hook.Companions;
            bool active = roster.Active == c;
            GUILayout.Label($"<color={BagPanel.RarityColor(c.Rarity)}>{c.Name}</color>  <size=12>{c.Rarity} · {c.Element} · {c.StageName}</size>", HuntStyles.Heading);
            GUILayout.Label($"<b>{c.Role}</b>{(active ? " · fights with you" : "")}: {CompanionRules.Describe(c.Role)}", HuntStyles.Hint);
            GUILayout.Label(c.Stats, HuntStyles.Text);

            GUILayout.Label($"Life: {c.AgeLeft} battles left", HuntStyles.Text);
            HuntStyles.Bar((float)c.AgeLeft / Mathf.Max(1, c.Lifespan), c.AgeLeft < 10 ? new Color(0.95f, 0.45f, 0.4f) : new Color(0.5f, 0.8f, 1f));
            if (c.Stage < GrowthStage.Elder)
            {
                int need = CompanionGrowth.GrowthToEvolve(c.Stage);
                GUILayout.Label($"Growth to evolve: {HuntStyles.Count(c.Growth, need)}  (feed it monster loot)", HuntStyles.Text);
                HuntStyles.Bar((float)c.Growth / need, new Color(0.45f, 0.85f, 0.5f));
            }
            else GUILayout.Label("Fully grown.", HuntStyles.Text);
            GUILayout.Label($"Potions drunk: {c.Infusions}/{CompanionGrowth.InfusionCap(c.Stage)}{(c.Echo != null ? $" · {c.Echo}" : "")}", HuntStyles.Hint);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(feeding == c ? "Close food" : "Feed", GUILayout.Height(30))) { feeding = feeding == c ? null : c; fusing = null; }
            GUI.enabled = roster.All.Count > 1;
            if (GUILayout.Button(fusing == c ? "Close fuse" : "Fuse", GUILayout.Height(30))) { fusing = fusing == c ? null : c; feeding = null; }
            GUI.enabled = true;
            if (GUILayout.Button($"Look: {CompanionGrowth.LookName(c.Look)}", GUILayout.Height(30))) c.Look = CompanionGrowth.NextLook(c.Look);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (c.Role == CompanionRole.Familiar)
            {
                GUI.enabled = !active;
                if (GUILayout.Button(active ? "In battle ⚔" : "Take into battle", GUILayout.Height(28))) roster.SetActive(c);
                GUI.enabled = true;
            }
            if (releasing == c)
            {
                if (HuntStyles.ColorButton("Yes, release", HuntStyles.Risky)) { roster.Release(c); releasing = null; hook.ApplyCompanionBonuses(); hook.Log($"Released {c.Name} back to the sea."); GUILayout.EndHorizontal(); return; }
                if (GUILayout.Button("Keep", GUILayout.Height(28))) releasing = null;
            }
            else if (GUILayout.Button("Release", GUILayout.Height(28))) releasing = c;
            GUILayout.EndHorizontal();
            GrowthRow(c);
        }

        void DrawChores()
        {
            var roster = hook.Companions;
            GUILayout.Label("What each role does", HuntStyles.Heading);
            GUILayout.Label("<b>Familiar</b>: fights with you.\n<b>Crew</b>: repairs the raft, harpoon and modules.\n<b>Mystic</b>: helps craft gear, potions, oils and hammers.", HuntStyles.Text);
            GUILayout.Space(6);
            if (roster.Best(CompanionRole.Crew) == null && roster.Best(CompanionRole.Mystic) == null)
            {
                GUILayout.Label("Tame a pet and make it Crew or Mystic to set its chores here.", HuntStyles.Hint);
                return;
            }
            if (roster.Best(CompanionRole.Crew) != null)
            {
                GUILayout.Label("Crew", HuntStyles.Heading);
                CompanionSettings.CrewBuilds = GUILayout.Toggle(CompanionSettings.CrewBuilds, " Build raft parts from the bag onto the raft");
                GUILayout.Label("Module repair levels are in Build (R) → Modules.", HuntStyles.Hint);
            }
            if (roster.Best(CompanionRole.Mystic) != null)
            {
                GUILayout.Label("Mystic", HuntStyles.Heading);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Keep at least {CompanionSettings.MysticKeep} of each potion and oil", HuntStyles.Text, GUILayout.Width(290));
                CompanionSettings.MysticKeep = Mathf.RoundToInt(GUILayout.HorizontalSlider(CompanionSettings.MysticKeep, 0, 5));
                GUILayout.EndHorizontal();
                CompanionSettings.MysticRefines = GUILayout.Toggle(CompanionSettings.MysticRefines, " Refine your equipped gear with essence when stocked");
            }
        }

        Companion releasing, feeding, fusing;
        bool catalyst;

        /// <summary>
        /// Phase 7 growth (GDD §10): Feed (loot grows it, potions infuse it), Fuse in the Transmutation
        /// Circle, and change its look. One companion's feed or fuse list is open at a time.
        /// </summary>
        void GrowthRow(Companion c)
        {
            var inv = hook.Inventory;
            if (feeding == c)
            {
                GUILayout.Label("<b>Pick food</b>: loot adds growth, potions add stats.", HuntStyles.Text);
                var food = inv.Items.Where(kv => CompanionGrowth.CanFeed(kv.Key)).ToList();
                if (food.Count == 0) GUILayout.Label("Nothing to feed: monster loot grows it, potions and oils make it stronger.", HuntStyles.Hint);
                foreach (var kv in food)
                {
                    var item = kv.Key;
                    string what = item.type == ItemType.Consumable ? "potion: +stat"
                        : $"+{CompanionGrowth.FeedPoints(item, c)} growth{(item.element == c.Element ? " (favourite!)" : "")}";
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    if (GUILayout.Button($"{kv.Value} × {item.displayName}  <size=10>{what}</size>", RichButton))
                    {
                        var r = CompanionGrowth.Feed(c, item);
                        if (r != FeedResult.CantEat && r != FeedResult.Full) inv.Remove(item, 1);
                        hook.Log(r == FeedResult.Evolved ? $"{c.Name} ate the {item.displayName} and evolved into an {c.StageName}! {c.Stats}"
                            : r == FeedResult.Grew ? $"{c.Name} ate the {item.displayName}. Growth {c.Growth}/{CompanionGrowth.GrowthToEvolve(c.Stage)}."
                            : r == FeedResult.Infused ? $"{c.Name} drank the {item.displayName}. {c.Stats}"
                            : r == FeedResult.Full ? $"{c.Name} is full: {(item.type == ItemType.Consumable ? "no more potions until it evolves" : "it is fully grown")}."
                            : $"{c.Name} won't eat that.");
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (fusing == c)
            {
                GUILayout.Label($"<b>Transmutation Circle</b>: pick a pet to fuse into {c.Name}. That pet is used up; {c.Name} keeps its echo.", HuntStyles.Text);
                foreach (var other in hook.Companions.All.Where(o => o != c).ToList())
                {
                    var rarity = CompanionGrowth.ResultRarity(c, other);
                    int cost = CompanionGrowth.CatalystCost(c, other);
                    bool canCat = inv.EssenceAtLeast(rarity) >= cost;
                    float chance = CompanionGrowth.FuseChance(c, other, catalyst && canCat);
                    string kind = c.Id == other.Id ? $"same kind → {rarity}" : $"hybrid → {CompanionGrowth.HybridName(c.Name, other.Name)}";
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    if (GUILayout.Button($"+ {other.Name} <size=10>({kind}, {chance:P0})</size>", RichButton))
                    {
                        bool useCat = catalyst && canCat;
                        if (useCat) inv.SpendEssence(rarity, cost);
                        string otherName = other.Name;
                        var r = CompanionGrowth.Fuse(c, other, useCat, Random.value);
                        hook.Companions.Release(other);
                        hook.ApplyCompanionBonuses();
                        hook.Log(r == FuseResult.Failed ? $"The circle fizzled: {otherName} slipped away into the sea. {c.Name} is unchanged."
                            : r == FuseResult.Upgraded ? $"{otherName} fused into {c.Name}: now {c.Rarity}! {c.Stats} · {c.Echo}"
                            : $"{otherName} fused into a hybrid: {c.Name} ({c.Rarity}). {c.Stats} · {c.Echo}");
                        fusing = null;
                        GUILayout.EndHorizontal();
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                var anyCost = hook.Companions.All.Where(o => o != c).Select(o => (CompanionGrowth.ResultRarity(c, o), CompanionGrowth.CatalystCost(c, o))).FirstOrDefault();
                catalyst = GUILayout.Toggle(catalyst, $" Use essence as a catalyst (+15%; {anyCost.Item2} {anyCost.Item1}+ essence)");
                GUILayout.EndHorizontal();
            }
        }

        GUIStyle RichButton => HuntStyles.RichButton;
    }
}
