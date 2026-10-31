using System;
using BrineBattle.Items;
using BrineBattle.Rafts;

namespace BrineBattle.Companions
{
    /// <summary>
    /// Who may craft what (Ed's pick, 2026-10-31): the hunter is an ordinary person, so crafting is the minions'
    /// alchemy. The hunter can still make Common things alone, so a new player isn't stuck before the first minion.
    /// Anything rarer, and every shaped module kit, needs a minion with the matching skill:
    /// gear and hammers a Smith or Tinker, potions, oils and treats an Alchemist, raft parts, modules, habitats,
    /// tree materials and ship parts a Builder or Crewmate, shaped kits a Builder or Alchemist.
    /// Taking things apart is always allowed. Plain C# for EditMode tests.
    /// </summary>
    public static class WhoCrafts
    {
        /// <summary>The skills that can make this recipe's result; empty when the hunter may make it alone.</summary>
        public static MinionSkill[] Needs(RecipeDefinition r)
        {
            if (r == null || r.kind == RecipeKind.Dismantle || r.outputs.Count == 0 || r.outputs[0].item == null) return Array.Empty<MinionSkill>();
            var item = r.outputs[0].item;
            if (ShapeRules.IsKit(item)) return new[] { MinionSkill.Builder, MinionSkill.Alchemist };
            if (item.rarity <= Rarity.Common) return Array.Empty<MinionSkill>();
            switch (item.type)
            {
                case ItemType.Weapon:
                case ItemType.Armor:
                case ItemType.Accessory:
                case ItemType.Tool:
                    return new[] { MinionSkill.Smith, MinionSkill.Tinker };
                case ItemType.Consumable:
                    return new[] { MinionSkill.Alchemist };
                default:
                    return new[] { MinionSkill.Builder, MinionSkill.Crewmate };
            }
        }

        /// <summary>
        /// The minion that makes it (the strongest with a needed skill), or null when the hunter makes it alone.
        /// <paramref name="allowed"/> is false when it needs a minion and none is there.
        /// </summary>
        public static Companion Maker(RecipeDefinition r, CompanionRoster roster, out bool allowed)
        {
            var needs = Needs(r);
            allowed = true;
            if (needs.Length == 0) return null;
            foreach (var s in needs)
            {
                var who = roster?.Best(s);
                if (who != null) return who;
            }
            allowed = false;
            return null;
        }

        /// <summary>"Needs a Smith or Tinker minion", for greyed rows (status only).</summary>
        public static string NeedText(RecipeDefinition r)
        {
            var needs = Needs(r);
            return needs.Length == 0 ? "" : $"Needs a {string.Join(" or ", needs)} minion";
        }
    }
}
