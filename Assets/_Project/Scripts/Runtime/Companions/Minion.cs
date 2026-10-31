using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Elements;
using BrineBattle.Items;

namespace BrineBattle.Companions
{
    /// <summary>
    /// What a minion can do (Ed's U-39). Every tamed monster is one kind of minion; it learns skills as it
    /// levels up by eating loot. Values are saved, so never renumber.
    /// </summary>
    public enum MinionSkill
    {
        /// <summary>Fights beside the hunter (can be the battle minion).</summary>
        Fighter = 0,
        /// <summary>Swims out for dead drifts and floating loot near the raft.</summary>
        Looter = 1,
        /// <summary>Installs modules and builds raft parts from the bag, patches the raft in battle, puts out fires.</summary>
        Crewmate = 2,
        /// <summary>Auto-repair: mends the harpoon and keeps modules above their repair line.</summary>
        Mender = 3,
        /// <summary>Auto-craft: keeps potions, oils and treats stocked; heals in battle; sometimes makes one extra.</summary>
        Alchemist = 4,
        /// <summary>Auto-upgrade: refines equipped weapon, armor and accessory; makes the next hammer; cheaper refines.</summary>
        Smith = 5,
        /// <summary>Eats the junk in the bag on its own and shares it with the hungriest minion.</summary>
        Glutton = 6,
        /// <summary>Dismantles or salvages junk up to the rarity set in Minions → Chores.</summary>
        Salvager = 7,
        /// <summary>Grows the raft toward the next level: reinforces parts, makes modules and vehicle parts, installs them, levels up.</summary>
        Builder = 8,
        /// <summary>Crafts weapons, armor and accessories better than what's equipped and equips them. Rare and up.</summary>
        Tinker = 9,
        /// <summary>Passive: +20% HP and DEF. Adult and up.</summary>
        Hardy = 10,
        /// <summary>Passive: +20% ATK and +1 SPD. Elder only.</summary>
        Ferocious = 11,
    }

    /// <summary>Kinds of food a minion can love (Claude's C-16). Saved as an int, so never renumber.</summary>
    public enum FoodKind { Materials = 0, RaftParts = 1, Gear = 2, ModulesAndTools = 3 }

    /// <summary>Minion levels, skills and food (Ed's U-39, GDD §10). Plain C# for EditMode tests.</summary>
    public static class MinionRules
    {
        public const int MaxLevel = 30;

        /// <summary>EXP from one level to the next: 5 + 5 × level (Lv 1 → 2 needs 10).</summary>
        public static int ExpToNext(int level) => level >= MaxLevel ? 0 : 5 + 5 * Math.Max(1, level);

        /// <summary>Evolution by level: Adult at 10, Elder at 20.</summary>
        public static GrowthStage StageAt(int level) => level >= 20 ? GrowthStage.Elder : level >= 10 ? GrowthStage.Adult : GrowthStage.Young;

        /// <summary>Levels at which a new skill slot opens (if the minion has room for it).</summary>
        public static readonly int[] LearnLevels = { 1, 3, 6, 9, 12, 15, 18, 22, 26, 30 };

        /// <summary>Skill slots opened by <paramref name="level"/>.</summary>
        public static int SlotsOpened(int level) => LearnLevels.Count(l => l <= level);

        /// <summary>Skills a minion can hold: 2 + rarity step + stage (Common Young 2 … Mythic Elder 9).</summary>
        public static int MaxSkills(Rarity r, GrowthStage s) => 2 + (int)r + (int)s;

        /// <summary>Strength of its work (repair, heal, fetch speed): rarity step + stage.</summary>
        public static int Rank(Companion c) => (int)c.Rarity + (int)c.Stage;

        public static readonly MinionSkill[] All = (MinionSkill[])Enum.GetValues(typeof(MinionSkill));

        /// <summary>Rarity or stage a skill needs, or null when any minion can learn it.</summary>
        public static string Gate(MinionSkill s)
        {
            switch (s)
            {
                case MinionSkill.Builder: return "Uncommon+";
                case MinionSkill.Tinker: return "Rare+";
                case MinionSkill.Hardy: return "Adult+";
                case MinionSkill.Ferocious: return "Elder";
                default: return null;
            }
        }

        public static bool CanLearn(Companion c, MinionSkill s)
        {
            if (c == null || c.Has(s)) return false;
            switch (s)
            {
                case MinionSkill.Builder: return c.Rarity >= Rarity.Uncommon;
                case MinionSkill.Tinker: return c.Rarity >= Rarity.Rare;
                case MinionSkill.Hardy: return c.Stage >= GrowthStage.Adult;
                case MinionSkill.Ferocious: return c.Stage >= GrowthStage.Elder;
                default: return true;
            }
        }

        /// <summary>The two skills a minion of this element leans toward; it learns them right after Fighter.</summary>
        public static MinionSkill[] Affinity(Element e)
        {
            switch (e)
            {
                case Element.Water: return new[] { MinionSkill.Looter, MinionSkill.Mender };
                case Element.Fire: return new[] { MinionSkill.Smith, MinionSkill.Ferocious };
                case Element.Earth: return new[] { MinionSkill.Crewmate, MinionSkill.Hardy };
                case Element.Wind: return new[] { MinionSkill.Looter, MinionSkill.Builder };
                case Element.Steam: return new[] { MinionSkill.Alchemist, MinionSkill.Tinker };
                case Element.Mud: return new[] { MinionSkill.Crewmate, MinionSkill.Salvager };
                case Element.Ice: return new[] { MinionSkill.Mender, MinionSkill.Alchemist };
                case Element.Magma: return new[] { MinionSkill.Smith, MinionSkill.Builder };
                case Element.Lightning: return new[] { MinionSkill.Tinker, MinionSkill.Ferocious };
                case Element.Sand: return new[] { MinionSkill.Salvager, MinionSkill.Glutton };
                default: return new[] { MinionSkill.Glutton, MinionSkill.Salvager };
            }
        }

        /// <summary>
        /// The order a minion learns in: Fighter, then its element's two skills, then the rest in an order
        /// fixed per monster kind (a stable hash of its id), so two kinds grow differently. Skills it can't
        /// learn yet (rarity or stage) are skipped and taken later when it can.
        /// </summary>
        public static IEnumerable<MinionSkill> Order(Companion c)
        {
            var first = new List<MinionSkill> { MinionSkill.Fighter };
            first.AddRange(Affinity(c.Element));
            var rest = All.Where(s => !first.Contains(s)).OrderBy(s => StableHash($"{c.Id}:{(int)s}"));
            return first.Concat(rest);
        }

        /// <summary>The next skill it would learn now, or null when nothing is learnable.</summary>
        public static MinionSkill? NextSkill(Companion c)
        {
            foreach (var s in Order(c)) if (CanLearn(c, s)) return s;
            return null;
        }

        /// <summary>Level at which the next skill slot opens, or 0 when it has no more room or slots.</summary>
        public static int NextLearnLevel(Companion c)
        {
            if (c.Skills.Count >= MaxSkills(c.Rarity, MinionSkillsStage(c))) return 0;
            foreach (var l in LearnLevels) if (l > c.Level && SlotsOpened(l) > c.Skills.Count) return l;
            return 0;
        }

        static GrowthStage MinionSkillsStage(Companion c) => (GrowthStage)Math.Max((int)c.Stage, (int)StageAt(c.Level));

        /// <summary>FNV-1a over the characters: the same on every run and machine (string.GetHashCode isn't).</summary>
        public static uint StableHash(string s)
        {
            uint h = 2166136261;
            foreach (char ch in s ?? "") { h ^= ch; h *= 16777619; }
            return h;
        }

        // --- Food (Ed's U-39: minions eat loot, mostly the low-quality stuff) ---

        /// <summary>Loot a minion can eat for EXP: materials, gear, raft parts, modules and tools. Never essence, potions (those infuse), habitats or vehicle parts.</summary>
        public static bool IsFood(ItemDefinition i) =>
            i != null && (i.type == ItemType.Material || i.type == ItemType.Raft || GearRules.IsGear(i.type)
                || i.type == ItemType.Module || i.type == ItemType.Tool);

        // ---- Favourite food and mood (Claude's C-16) ----

        /// <summary>A minion that hasn't had its favourite food for this long (10 min) sulks and works 20% slower.</summary>
        public const float SulkSeconds = 600f;

        public static FoodKind KindOf(ItemDefinition i) =>
            GearRules.IsGear(i.type) ? FoodKind.Gear : i.type == ItemType.Material ? FoodKind.Materials : i.type == ItemType.Raft ? FoodKind.RaftParts : FoodKind.ModulesAndTools;

        public static bool IsFavourite(ItemDefinition i, Companion c) => c != null && c.Favourite >= 0 && IsFood(i) && (int)KindOf(i) == c.Favourite;

        /// <summary>A favourite for a minion from an older save: picked from its id, so it stays the same.</summary>
        public static int FavouriteFromId(string id)
        {
            int h = 17;
            foreach (char ch in id ?? "") h = unchecked(h * 31 + ch);
            return (h & 0x7fffffff) % 4;
        }

        // ---- Auto-feed (Ed's ask: in Auto, minions feed themselves, with a cooldown so they don't overeat) ----

        /// <summary>Seconds between one minion's snacks.</summary>
        public const float AutoFeedSeconds = 45f;

        /// <summary>
        /// What a minion eats on its own: food only (never hammers), of a rarity that's ticked, and only while
        /// <paramref name="share"/> of the stack is more than one item (so it never eats the last of something you
        /// keep). Its favourite food first, then the commonest rarity, then the biggest stack. Null when nothing fits.
        /// </summary>
        public static ItemDefinition AutoFeedPick(IEnumerable<KeyValuePair<ItemDefinition, int>> bag, Companion c, Func<Rarity, bool> tierOn, float share)
        {
            if (c == null || c.Level >= MaxLevel) return null;
            return bag.Where(kv => kv.Key != null && IsFood(kv.Key) && kv.Key.type != ItemType.Tool && tierOn(kv.Key.rarity)
                    && Math.Floor(kv.Value * Math.Max(0f, Math.Min(1f, share))) >= 1)
                .OrderByDescending(kv => IsFavourite(kv.Key, c)).ThenBy(kv => kv.Key.rarity).ThenByDescending(kv => kv.Value)
                .Select(kv => kv.Key).FirstOrDefault();
        }

        /// <summary>Job speed: 0.8 while sulking, else 1.</summary>
        public static float WorkSpeed(Companion c) => c != null && c.Sulking ? 0.8f : 1f;

        public static string FoodName(int kind)
        {
            switch ((FoodKind)kind)
            {
                case FoodKind.Materials: return "materials";
                case FoodKind.RaftParts: return "raft parts";
                case FoodKind.Gear: return "gear";
                case FoodKind.ModulesAndTools: return "modules and hammers";
                default: return "anything";
            }
        }

        /// <summary>EXP one item gives: (1 + rarity step) × 1 material / 2 part, module or tool / 3 gear; double when its element matches.</summary>
        public static int ExpFor(ItemDefinition i, Companion c)
        {
            if (!IsFood(i)) return 0;
            int kind = GearRules.IsGear(i.type) ? 3 : i.type == ItemType.Material ? 1 : 2;
            int p = (1 + (int)i.rarity) * kind;
            return c != null && i.element != Element.None && i.element == c.Element ? p * 2 : p;
        }

        /// <summary>
        /// Junk the minions may eat or take apart on their own (Ed's U-39): gear below its tier, raft parts and
        /// modules below the raft's level, and hammers below the best one. Never materials or potions (the raft
        /// level-up needs materials), and never parts or modules a recipe still uses (<paramref name="isIngredient"/>).
        /// </summary>
        public static bool IsJunk(ItemDefinition i, Func<ItemType, Rarity> tierOf, Rarity raftTier, Rarity? bestHammer,
            Func<ItemDefinition, bool> isIngredient = null)
        {
            if (i == null) return false;
            switch (i.type)
            {
                case ItemType.Weapon:
                case ItemType.Armor:
                    return i.rarity < tierOf(i.type);
                case ItemType.Accessory:
                    return i.rarity < raftTier;
                case ItemType.Raft:
                case ItemType.Module:
                    return i.rarity < raftTier && (isIngredient == null || !isIngredient(i));
                case ItemType.Tool:
                    return bestHammer != null && i.rarity < bestHammer.Value;
                default:
                    return false;
            }
        }

        public enum JunkAction { Dismantle, Salvage, Eat }

        /// <summary>
        /// What happens to one junk item (Ed's ask): at or under the Chores rarity it is taken apart, with a
        /// dismantle recipe if there is one (materials back), otherwise salvaged into essence; above it, it is eaten.
        /// </summary>
        public static JunkAction ForJunk(ItemDefinition i, Rarity takeApartUpTo, bool hasDismantle) =>
            i.rarity > takeApartUpTo ? JunkAction.Eat : hasDismantle ? JunkAction.Dismantle : JunkAction.Salvage;

        // --- Skill tree (Ed's ask: four branches) ---

        public enum Family { Battle, Crew, Alchemy, Looter }
        public static readonly Family[] Families = { Family.Battle, Family.Crew, Family.Alchemy, Family.Looter };

        public static Family FamilyOf(MinionSkill s)
        {
            switch (s)
            {
                case MinionSkill.Fighter: case MinionSkill.Hardy: case MinionSkill.Ferocious: return Family.Battle;
                case MinionSkill.Crewmate: case MinionSkill.Mender: case MinionSkill.Builder: return Family.Crew;
                case MinionSkill.Alchemist: case MinionSkill.Smith: case MinionSkill.Tinker: return Family.Alchemy;
                default: return Family.Looter;
            }
        }

        public static IEnumerable<MinionSkill> In(Family f) => All.Where(s => FamilyOf(s) == f);

        public enum NodeState { Learned, Next, Later, Locked }

        /// <summary>A skill's place on the tree for this minion: learned, the next one (at <paramref name="atLevel"/>), later, or locked by rarity/stage/room.</summary>
        public static NodeState StateOf(Companion c, MinionSkill s, out int atLevel)
        {
            atLevel = 0;
            if (c.Has(s)) return NodeState.Learned;
            if (Gate(s) != null && !CanLearn(c, s) && !GateMetLater(c, s)) return NodeState.Locked;
            if (NextSkill(c) == s) { atLevel = NextLearnLevel(c); return atLevel > 0 ? NodeState.Next : NodeState.Locked; }
            return NodeState.Later;
        }

        /// <summary>A stage gate opens by leveling (Adult 10, Elder 20); a rarity gate only by fusion.</summary>
        static bool GateMetLater(Companion c, MinionSkill s) => s == MinionSkill.Hardy || s == MinionSkill.Ferocious;

        // --- Text ---

        public static string Describe(MinionSkill s)
        {
            switch (s)
            {
                case MinionSkill.Fighter: return "fights beside you";
                case MinionSkill.Looter: return "fetches dead drifts and floating loot";
                case MinionSkill.Crewmate: return "installs modules and builds parts from the bag, patches the raft in battle, puts out fires";
                case MinionSkill.Mender: return "mends the harpoon and repairs modules";
                case MinionSkill.Alchemist: return "keeps potions, oils and treats stocked, heals in battle";
                case MinionSkill.Smith: return "refines your gear and makes the next hammer";
                case MinionSkill.Glutton: return "eats junk from the bag and shares it";
                case MinionSkill.Salvager: return "dismantles or salvages junk";
                case MinionSkill.Builder: return "reinforces raft parts, makes modules and vehicle parts, levels the raft or ship up";
                case MinionSkill.Tinker: return "crafts better weapons, armor and accessories and equips them";
                case MinionSkill.Hardy: return "+20% HP and DEF";
                default: return "+20% ATK, +1 SPD";
            }
        }

        public static string Skills(Companion c) => c.Skills.Count == 0 ? "no skills yet" : string.Join(", ", c.Skills);
    }
}
