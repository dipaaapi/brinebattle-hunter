using System.Collections.Generic;
using System;
using BrineBattle.Items;

namespace BrineBattle.Companions
{
    /// <summary>What feeding did.</summary>
    public enum FeedResult { CantEat, Grew, LevelUp, Evolved, Infused, Full }

    /// <summary>The outcome of a fusion in the Transmutation Circle.</summary>
    public enum FuseResult { Upgraded, Hybrid, Failed, NotAllowed }

    /// <summary>
    /// Growing minions (GDD §10, Ed's U-39): eating loot gives EXP and levels (<see cref="MinionRules"/>), potions
    /// infuse, fusing two in the Transmutation Circle (the source is consumed and leaves an echo and one skill),
    /// and their looks. Plain C# for EditMode tests.
    /// </summary>
    public static class CompanionGrowth
    {
        // --- Feeding / infusion ---

        /// <summary>EXP one item gives this minion (see <see cref="MinionRules.ExpFor"/>).</summary>
        public static int FeedPoints(ItemDefinition item, Companion c) => MinionRules.ExpFor(item, c) * (MinionRules.IsFavourite(item, c) ? 2 : 1);

        /// <summary>Potions a companion can take: 3 when Young, +2 per stage.</summary>
        public static int InfusionCap(GrowthStage stage) => 3 + 2 * (int)stage;

        /// <summary>Loot feeds (materials, gear, raft parts, modules, tools); consumables infuse.</summary>
        public static bool CanFeed(ItemDefinition item) =>
            MinionRules.IsFood(item) || (item != null && item.type == ItemType.Consumable);

        public static FeedResult Feed(Companion c, ItemDefinition item) => Feed(c, item, null);

        /// <summary>
        /// Feeds one <paramref name="item"/> to <paramref name="c"/>. Loot gives EXP (levels, evolutions at
        /// Lv 10/20 and new skills, listed in <paramref name="learned"/>); a consumable infuses a permanent stat:
        /// heals +3 HP, oils +1 ATK, raft patches +1 DEF. The caller takes the item from the bag unless the result
        /// is CantEat or Full.
        /// </summary>
        public static FeedResult Feed(Companion c, ItemDefinition item, List<MinionSkill> learned)
        {
            if (c == null || !CanFeed(item)) return FeedResult.CantEat;
            if (item.type == ItemType.Consumable)
            {
                if (c.Infusions >= InfusionCap(c.Stage)) return FeedResult.Full;
                c.Infusions++;
                switch (item.effect)
                {
                    case ConsumableEffect.CoatWeapon: c.Attack += 1; break;
                    case ConsumableEffect.RepairRaft: c.Defense += 1; break;
                    default: c.MaxHp += 3; break;
                }
                return FeedResult.Infused;
            }
            if (c.Level >= MinionRules.MaxLevel) return FeedResult.Full;
            var stage = c.Stage;
            int levels = c.GainExp(FeedPoints(item, c), learned);
            if (MinionRules.IsFavourite(item, c)) c.SinceFavourite = 0f;   // its favourite (C-16): happy again
            return c.Stage > stage ? FeedResult.Evolved : levels > 0 ? FeedResult.LevelUp : FeedResult.Grew;
        }

        // --- Fusion (U-8 + C-5) ---

        /// <summary>Chance a fusion works: same kind 85%, a hybrid 65%; a catalyst adds 15%.</summary>
        public static float FuseChance(Companion a, Companion b, bool catalyst) =>
            Math.Min(1f, (a.Id == b.Id ? 0.85f : 0.65f) + (catalyst ? 0.15f : 0f));

        /// <summary>Essence of the result's rarity (or rarer) a catalyst costs: 2 + rarity step.</summary>
        public static int CatalystCost(Companion a, Companion b) => 2 + (int)ResultRarity(a, b);

        /// <summary>Same kind steps up one rarity (up to Mythic); a hybrid keeps the rarer one's rarity.</summary>
        public static Rarity ResultRarity(Companion a, Companion b) =>
            a.Id == b.Id ? (Rarity)Math.Min((int)Rarity.Mythic, (int)a.Rarity + 1) : (Rarity)Math.Max((int)a.Rarity, (int)b.Rarity);

        /// <summary>Both are in the roster and not the same one.</summary>
        public static bool CanFuse(Companion a, Companion b) => a != null && b != null && a != b;

        /// <summary>
        /// Fuses <paramref name="source"/> into <paramref name="keep"/>. <paramref name="roll"/> is 0–1 (random).
        /// Success: same kind → one rarity up, each stat the better of the two ×1.15; different kinds → a hybrid
        /// (name from both, each stat the better ×1.05). Either way <paramref name="keep"/> gains an echo of the
        /// source: a quarter of its HP, ATK and DEF, and the higher stage. Failure: the source swims away.
        /// The source is always used up (the caller removes it from the roster).
        /// </summary>
        public static FuseResult Fuse(Companion keep, Companion source, bool catalyst, double roll)
        {
            if (!CanFuse(keep, source)) return FuseResult.NotAllowed;
            if (roll >= FuseChance(keep, source, catalyst)) return FuseResult.Failed;
            bool same = keep.Id == source.Id;
            float boost = same ? 1.15f : 1.05f;
            var rarity = ResultRarity(keep, source);
            if (!same)
            {
                keep.Id = $"{keep.Id}+{source.Id}";
                keep.Name = HybridName(keep.Name, source.Name);
            }
            keep.Rarity = rarity;
            keep.MaxHp = Better(keep.MaxHp, source.MaxHp, boost) + source.MaxHp / 4;
            keep.Attack = Better(keep.Attack, source.Attack, boost) + source.Attack / 4;
            keep.Defense = Better(keep.Defense, source.Defense, boost) + source.Defense / 4;
            keep.Speed = Math.Max(keep.Speed, source.Speed);
            if (source.Stage > keep.Stage) keep.Stage = source.Stage;
            if (source.Level > keep.Level) { keep.Level = source.Level; keep.Exp = 0; }
            keep.Echo = $"Echo of {source.Name}";
            // The echo carries one skill over (U-39), even past the keeper's usual room.
            var gift = EchoSkill(keep, source);
            if (gift != null) keep.Learn(gift.Value);
            keep.CatchUp();
            return same ? FuseResult.Upgraded : FuseResult.Hybrid;
        }

        /// <summary>The skill a fusion passes on: the source's first learned skill the keeper doesn't have, or null.</summary>
        public static MinionSkill? EchoSkill(Companion keep, Companion source)
        {
            foreach (var s in source.Skills) if (!keep.Has(s)) return s;
            return null;
        }

        static int Better(int a, int b, float boost) => Math.Max(Math.Max(a, b) + 1, (int)Math.Round(Math.Max(a, b) * boost));

        /// <summary>"Brinefang Eel" + "Gale Gull" → "Brinefang Gull".</summary>
        public static string HybridName(string a, string b)
        {
            string first = a.Split(' ')[0], last = b.Split(' ')[b.Split(' ').Length - 1];
            return first == last ? a : $"{first} {last}";
        }

        // --- Looks (U-23 varieties, cosmetic only) ---

        public static readonly string[] Looks = { "Plain", "Scarf", "Bandana", "Crown" };
        public static string LookName(int look) => Looks[((look % Looks.Length) + Looks.Length) % Looks.Length];
        public static int NextLook(int look) => (look + 1) % Looks.Length;
    }
}
