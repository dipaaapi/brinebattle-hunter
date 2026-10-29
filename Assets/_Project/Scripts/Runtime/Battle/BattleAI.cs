using System.Linq;
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
        /// Null = no item this round. Manual never uses items by itself.
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
            return null;
        }

        public static BattleAction? Choose(BattleState s, BattleMode mode)
        {
            if (mode == BattleMode.Manual) return null;
            bool low = s.Hunter.Hp <= s.Hunter.MaxHp * LowHp;
            bool harpoonReady = s.CanUse(BattleAction.Harpoon);

            if (mode == BattleMode.SemiAuto)
                // Auto basic attacks; the player decides the important turns.
                return low || harpoonReady ? (BattleAction?)null : BattleAction.Attack;

            if (low && !harpoonReady) return BattleAction.Brace;
            if (harpoonReady) return BattleAction.Harpoon;
            return BattleAction.Attack;
        }
    }
}
