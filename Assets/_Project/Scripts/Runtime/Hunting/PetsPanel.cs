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

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!open || hook == null) return;
            GUI.depth = -5;
            float w = Mathf.Min(600, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(560, HuntStyles.H - 40);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), "Pets", HuntStyles.Window);
            scroll = GUILayout.BeginScrollView(scroll);
            CompanionRows();
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close (P)")) WorldPause.Close(Menu.Pets);
            GUILayout.EndArea();
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
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            string grow = c.Stage >= GrowthStage.Elder ? "fully grown" : $"growth {c.Growth}/{CompanionGrowth.GrowthToEvolve(c.Stage)}";
            GUILayout.Label($"<size=10>{grow} · potions {c.Infusions}/{CompanionGrowth.InfusionCap(c.Stage)}{(c.Echo != null ? $" · {c.Echo}" : "")}</size>", Rich, GUILayout.Width(250));
            if (GUILayout.Button(feeding == c ? "Done" : "Feed", GUILayout.Width(55))) { feeding = feeding == c ? null : c; fusing = null; }
            GUI.enabled = hook.Companions.All.Count > 1;
            if (GUILayout.Button(fusing == c ? "Done" : "Fuse", GUILayout.Width(55))) { fusing = fusing == c ? null : c; feeding = null; }
            GUI.enabled = true;
            if (GUILayout.Button($"Look: {CompanionGrowth.LookName(c.Look)}", GUILayout.Width(110))) c.Look = CompanionGrowth.NextLook(c.Look);
            GUILayout.EndHorizontal();

            if (feeding == c)
            {
                var food = inv.Items.Where(kv => CompanionGrowth.CanFeed(kv.Key)).ToList();
                if (food.Count == 0) GUILayout.Label("    <size=11>Nothing to feed: monster loot grows it, potions and oils make it stronger.</size>", Rich);
                foreach (var kv in food)
                {
                    var item = kv.Key;
                    string what = item.type == ItemType.Consumable ? "potion: +stat"
                        : $"+{CompanionGrowth.FeedPoints(item, c)} growth{(item.element == c.Element ? " (favourite!)" : "")}";
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(24);
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
                GUILayout.Label($"    <b>Transmutation Circle</b> <size=11>— pick a companion to fuse into {c.Name}. It is used up; {c.Name} keeps its echo.</size>", Rich);
                foreach (var other in hook.Companions.All.Where(o => o != c).ToList())
                {
                    var rarity = CompanionGrowth.ResultRarity(c, other);
                    int cost = CompanionGrowth.CatalystCost(c, other);
                    bool canCat = inv.EssenceAtLeast(rarity) >= cost;
                    float chance = CompanionGrowth.FuseChance(c, other, catalyst && canCat);
                    string kind = c.Id == other.Id ? $"same kind → {rarity}" : $"hybrid → {CompanionGrowth.HybridName(c.Name, other.Name)}";
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(24);
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
                GUILayout.Space(24);
                var anyCost = hook.Companions.All.Where(o => o != c).Select(o => (CompanionGrowth.ResultRarity(c, o), CompanionGrowth.CatalystCost(c, o))).FirstOrDefault();
                catalyst = GUILayout.Toggle(catalyst, $" Use essence as a catalyst (+15%; {anyCost.Item2} {anyCost.Item1}+ essence)");
                GUILayout.EndHorizontal();
            }
        }

        GUIStyle richButton;
        GUIStyle RichButton => richButton ??= new GUIStyle(GUI.skin.button) { richText = true, alignment = TextAnchor.MiddleLeft };

        /// <summary>Phase 7: tamed companions; the active one joins battles. Release asks twice.</summary>
        void CompanionRows()
        {
            var roster = hook.Companions;
            GUILayout.Label($"<b>Companions</b>  ({roster.All.Count}/{roster.Capacity}; habitats on the raft add room later)", Rich);
            GUILayout.Label("<size=10>Familiar fights with you · Crew repairs the raft, harpoon and modules · Mystic helps craft gear, potions and oils</size>", Rich);
            if (roster.Best(CompanionRole.Crew) != null)
                CompanionSettings.CrewBuilds = GUILayout.Toggle(CompanionSettings.CrewBuilds, " Crew builds raft parts from the bag onto the raft");
            if (roster.Best(CompanionRole.Mystic) != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Mystic keeps at least {CompanionSettings.MysticKeep} of each potion and oil", GUILayout.Width(300));
                CompanionSettings.MysticKeep = Mathf.RoundToInt(GUILayout.HorizontalSlider(CompanionSettings.MysticKeep, 0, 5));
                GUILayout.EndHorizontal();
                CompanionSettings.MysticRefines = GUILayout.Toggle(CompanionSettings.MysticRefines, " Mystic refines your equipped gear with essence when stocked");
            }
            if (roster.All.Count == 0) GUILayout.Label("  (none — tame a weakened, calm drift)");
            foreach (var c in roster.All.ToList())
            {
                bool active = roster.Active == c;
                GUILayout.BeginHorizontal();
                string tip = $"{c.Role}: {CompanionRules.Describe(c.Role)}";
                GUILayout.Label(new GUIContent($"<color={BagPanel.RarityColor(c.Rarity)}>{c.Name}</color> <b>{c.Role}</b>{(active ? " (in battle)" : "")}  <size=10>{c.Element} · {c.StageName}, {c.AgeLeft} battles left · {c.Stats}</size>", tip), Rich, GUILayout.Width(360));
                if (c.Role == CompanionRole.Familiar)
                {
                    GUI.enabled = !active;
                    if (GUILayout.Button("Set active", GUILayout.Width(80))) roster.SetActive(c);
                    GUI.enabled = true;
                }
                else GUILayout.Label("", GUILayout.Width(80));
                if (releasing == c)
                {
                    if (GUILayout.Button("Sure?", GUILayout.Width(55))) { roster.Release(c); releasing = null; hook.ApplyCompanionBonuses(); hook.Log($"Released {c.Name} back to the sea."); }
                }
                else if (GUILayout.Button("Release", GUILayout.Width(65))) releasing = c;
                GUILayout.EndHorizontal();
                GrowthRow(c);
            }
            GUILayout.Space(4);
        }


        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
    }
}
