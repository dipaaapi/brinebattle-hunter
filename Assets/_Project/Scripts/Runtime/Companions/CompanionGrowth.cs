using System;
using BrineBattle.Items;

namespace BrineBattle.Companions
{
    /// <summary>What feeding did.</summary>
    public enum FeedResult { CantEat, Grew, Evolved, Infused, Full }

    /// <summary>The outcome of a fusion in the Transmutation Circle.</summary>
    public enum FuseResult { Upgraded, Hybrid, Failed, NotAllowed }

    /// <summary>
    /// Growing companions without EXP (GDD §10): feeding loot and infusing potions, fusing two in the
    /// Transmutation Circle (the source is consumed and leaves an echo), and their looks. Plain C# for EditMode tests.
    /// </summary>
    public static class CompanionGrowth
    {
        // --- Feeding / infusion ---

        /// <summary>Feeding points needed for the next evolution: Young 10, Adult 20.</summary>
        public static int GrowthToEvolve(GrowthStage stage) => 10 * ((int)stage + 1);

        /// <summary>Points a material gives: 1 + its rarity step, doubled when its element matches the companion's.</summary>
        public static int FeedPoints(ItemDefinition item, Companion c)
        {
            int p = 1 + (int)item.rarity;
            return item.element == c.Element && item.element != Elements.Element.None ? p * 2 : p;
        }

        /// <summary>Potions a companion can take: 3 when Young, +2 per stage.</summary>
        public static int InfusionCap(GrowthStage stage) => 3 + 2 * (int)stage;

        /// <summary>Monster loot (materials) feeds; consumables infuse. Gear, raft parts and modules aren't food.</summary>
        public static bool CanFeed(ItemDefinition item) =>
            item != null && (item.type == ItemType.Material || item.type == ItemType.Consumable);

        /// <summary>
        /// Feeds one <paramref name="item"/> to <paramref name="c"/>. Materials add growth (an evolution when it
        /// fills up); a consumable infuses a permanent stat: heals +3 HP, oils +1 ATK, raft patches +1 DEF.
        /// The caller takes the item from the bag unless the result is CantEat or Full.
        /// </summary>
        public static FeedResult Feed(Companion c, ItemDefinition item)
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
            if (c.Stage >= GrowthStage.Elder) return FeedResult.Full;
            c.Growth += FeedPoints(item, c);
            if (c.Growth < GrowthToEvolve(c.Stage)) return FeedResult.Grew;
            c.Growth = 0;
            c.Evolve();
            return FeedResult.Evolved;
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
            keep.Echo = $"Echo of {source.Name}";
            return same ? FuseResult.Upgraded : FuseResult.Hybrid;
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
