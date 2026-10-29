namespace BrineBattle.Items
{
    /// <summary>Compatibility and salvage (GDD §11). Plain C# for EditMode tests.</summary>
    public static class SalvageRules
    {
        /// <summary>Types that go onto something with a tier (raft, weapon, armor, companion).</summary>
        /// <summary>
        /// Items whose rarity must fit the target's tier. Auto-loot modules are not among them: their
        /// strength is their level, and a raft that levels up still needs modules on its left edge (Ed's playtest).
        /// </summary>
        public static bool IsEquippable(ItemType t) =>
            t == ItemType.Raft || t == ItemType.Weapon || t == ItemType.Armor || t == ItemType.Companion;

        /// <summary>
        /// An equippable item fits only if its rarity is at least the target's tier
        /// (an Uncommon raft part can't go on an Epic raft). Other types always fit.
        /// </summary>
        public static bool IsCompatible(ItemType type, Rarity itemRarity, Rarity targetTier) =>
            !IsEquippable(type) || itemRarity >= targetTier;

        /// <summary>Essence per salvaged item: Common 1, Uncommon 2 ... Mythic 6. Essence itself can't be salvaged.</summary>
        public static int EssenceYield(ItemType type, Rarity rarity) =>
            type == ItemType.Essence ? 0 : (int)rarity + 1;
    }
}
