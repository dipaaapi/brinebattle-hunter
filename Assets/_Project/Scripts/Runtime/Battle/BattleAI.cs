using System.Linq;
using BrineBattle.Elements;
using BrineBattle.Items;

namespace BrineBattle.Battle
{
    /// <summary>Picks the hunter's action for Semi-auto and Auto. Null means "wait for the player".</summary>
    public static class BattleAI
    {
        public const float LowHp = 0.35f;

        /// <summary>
        /// Semi-auto and Auto use consumables on their own: a raft repair when the raft is at
        /// <see cref="LowHp"/> or below, a heal when the hunter is. The lower side goes first.
        /// Auto also coats the weapon with an elemental oil that hits the monster harder than
        /// the current element. Null = no item this round. Manual never uses items by itself.
        /// </summary>
        public static ItemDefinition ChooseItem(BattleState s, BattleMode mode)
        {
            if (mode == BattleMode.Manual || s.Bag == null) return null;
            float raft = s.RaftHp / (float)s.RaftMaxHp;
            float hunter = s.Hunter.Hp / (float)s.Hunter.MaxHp;
            var order = raft <= hunter
                ? new[] { (raft, ConsumableEffect.RepairRaft), (hunter, ConsumableEffect.HealHunter) }
                : new[] { (hunter, ConsumableEffect.HealHunter), (raft, ConsumableEffect.RepairRaft) };
            foreach (var (ratio, effect) in order)
            {
                if (ratio > LowHp) continue;
                var item = s.Bag.Items.Select(kv => kv.Key).FirstOrDefault(i => i.effect == effect && s.CanUseItem(i));
                if (item != null) return item;
            }
            return mode == BattleMode.Auto ? BestOil(s) : null;
        }

        /// <summary>The usable oil with the best damage multiplier against the monster, if it beats the current one.</summary>
        public static ItemDefinition BestOil(BattleState s)
        {
            float now = ElementRules.Multiplier(s.AttackElement, s.Monster.Element);
            return s.Bag?.Items.Select(kv => kv.Key)
                .Where(i => i.effect == ConsumableEffect.CoatWeapon && s.CanUseItem(i))
                .Where(i => ElementRules.Multiplier(i.element, s.Monster.Element) > now)
                .OrderByDescending(i => ElementRules.Multiplier(i.element, s.Monster.Element))
                .FirstOrDefault();
        }

        /// <summary>
        /// Auto uses gear actives: Shell Guard when the raft is low, otherwise Tidal Strike when affordable.
        /// None = no skill this round. Semi-auto leaves skills to the player (see <see cref="Choose"/>).
        /// </summary>
        public static GearActive ChooseSkill(BattleState s, BattleMode mode)
        {
            if (mode != BattleMode.Auto) return GearActive.None;
            if (s.RaftHp <= s.RaftMaxHp * LowHp && s.CanUseSkill(GearActive.ShellGuard)) return GearActive.ShellGuard;
            if (s.CanUseSkill(GearActive.TidalStrike)) return GearActive.TidalStrike;
            return GearActive.None;
        }

        public static BattleAction? Choose(BattleState s, BattleMode mode)
        {
            if (mode == BattleMode.Manual) return null;
            bool low = s.Hunter.Hp <= s.Hunter.MaxHp * LowHp;
            bool harpoonReady = s.CanUse(BattleAction.Harpoon);

            if (mode == BattleMode.SemiAuto)
                // Auto basic attacks; the player decides the important turns (low HP, harpoon or a gear skill ready).
                return low || harpoonReady || s.Actives.Any(s.CanUseSkill) ? (BattleAction?)null : BattleAction.Attack;

            if (low && !harpoonReady) return BattleAction.Brace;
            if (harpoonReady) return BattleAction.Harpoon;
            return BattleAction.Attack;
        }
    }
}
