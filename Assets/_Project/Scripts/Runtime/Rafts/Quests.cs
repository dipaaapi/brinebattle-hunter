using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>Where a quest's "Go" button leads.</summary>
    public enum QuestTarget { None, RaftTree, Build, Workbench, Leave, Boss }

    /// <summary>One requirement line: what it is, have/need, and whether it is met.</summary>
    public class QuestLine
    {
        public string Text;
        public int Have, Need;
        public bool Done => Have >= Need;
        public QuestLine(string text, int have, int need) { Text = text; Have = have; Need = need; }
    }

    /// <summary>A quest built from game state (Ed's ask): what's missing to upgrade or craft the next thing.</summary>
    public class Quest
    {
        public string Id, Title, Summary;
        public QuestTarget Target;
        /// <summary>The Workbench sub-tab when <see cref="Target"/> is Workbench.</summary>
        public RecipeSub Sub;
        public List<QuestLine> Lines = new List<QuestLine>();
        public bool Done => Lines.Count > 0 && Lines.All(l => l.Done);
        public int Missing => Lines.Count(l => !l.Done);
    }

    /// <summary>
    /// The quest list (Ed's ask: a Quest tab apart from the workbench). Every quest comes from the raft, the
    /// bag and the recipes, so it is always current: next raft level, the level's boss, the raft type, modules,
    /// the hammer, the next vehicle part and leaving the sea. Plain C# for EditMode tests.
    /// </summary>
    public static class QuestRules
    {
        public static List<Quest> Build(RaftGrid raft, Inventory bag, IEnumerable<RecipeDefinition> recipes)
        {
            var list = new List<Quest>();
            var known = (recipes ?? Enumerable.Empty<RecipeDefinition>()).Where(r => r != null && r.kind != RecipeKind.Dismantle && r.outputs.Count > 0 && r.outputs[0].item != null).ToList();
            if (raft.CanChoose || (!raft.IsShip && raft.AtMax))
            {
                var q = new Quest { Id = "type", Title = "Pick the ship line", Summary = "Sea level (STR), Aerial (INT) or Undersea (DEX)", Target = QuestTarget.RaftTree };
                q.Lines.Add(new QuestLine($"Raft Lv {EvolutionRules.RaftMax}", raft.Level, EvolutionRules.RaftMax));
                if (raft.BossesNeeded) q.Lines.Add(new QuestLine($"{raft.Tier} boss beaten", raft.BossBeaten(raft.Tier) ? 1 : 0, 1));
                q.Lines.Add(new QuestLine("Ship line picked", 0, 1));
                list.Add(q);
            }

            if (!raft.AtMax)
            {
                var q = new Quest { Id = "level", Title = EvolutionRules.Label(raft.Chosen, raft.Level + 1), Target = QuestTarget.RaftTree };
                q.Lines.Add(new QuestLine("Monsters beaten", raft.Kills, raft.KillsNeeded));
                q.Lines.Add(new QuestLine("Monster materials", bag.MaterialCount(), raft.MaterialsNeeded));
                q.Lines.Add(new QuestLine($"{raft.Tier}+ essence", bag.EssenceAtLeast(raft.Tier), raft.EssenceNeeded));
                q.Lines.Add(new QuestLine("Modules on the left edge", raft.UpstreamModules().guarded, raft.ModulesNeeded));
                q.Lines.Add(new QuestLine($"{raft.Tier} hammer or better", RaftRules.HasHammer(bag, raft.Tier) ? 1 : 0, 1));
                if (raft.BossesNeeded && EvolutionRules.BossGate(raft.Chosen, raft.Level)) q.Lines.Add(new QuestLine($"{raft.Tier} boss beaten", raft.BossBeaten(raft.Tier) ? 1 : 0, 1));
                int part = EvolutionRules.PartNeeded(raft.Chosen, raft.Level);
                if (part > 0) q.Lines.Add(new QuestLine($"{VesselRules.PartName(raft.Chosen, part)} on", raft.HasVessel(part) ? 1 : 0, 1));
                q.Summary = q.Done ? "Ready: level up in the Raft tree" : $"{raft.FormName} · {q.Missing} left";
                list.Add(q);
            }
            else if (raft.IsShip)
                list.Add(new Quest { Id = "next", Title = $"Next evolution: {EvolutionRules.NextEvolution}", Summary = $"Your {raft.FormName} is Lv {EvolutionRules.ShipMax}",
                    Lines = { new QuestLine($"{EvolutionRules.LineName(raft.Chosen)} Lv {EvolutionRules.ShipMax}", raft.Level, EvolutionRules.ShipMax) } });

            if (raft.BossPending)
                list.Add(new Quest { Id = "boss", Title = $"Beat the {raft.Tier} boss", Target = QuestTarget.Boss,
                    Summary = "Once the level's monsters are beaten it shows up on its own, or summon it from here when you're ready",
                    Lines = { new QuestLine("Monsters beaten", raft.Kills, raft.KillsNeeded), new QuestLine("Boss beaten", 0, 1) } });

            if (!raft.ModulesReady)
            {
                var q = new Quest { Id = "modules", Title = "Guard the left edge", Target = QuestTarget.Workbench, Sub = RecipeSub.Modules,
                    Summary = "Auto-loot modules on the left edge" };
                q.Lines.Add(new QuestLine("Modules on the left edge", raft.UpstreamModules().guarded, raft.ModulesNeeded));
                var module = known.FirstOrDefault(r => r.outputs[0].item.type == ItemType.Module);
                if (module != null) AddIngredients(q, module, bag);
                list.Add(q);
            }

            if (!RaftRules.HasHammer(bag, raft.Tier))
            {
                var best = RaftRules.BestHammer(bag);
                var next = best == null ? Rarity.Common : best.rarity + 1;
                var recipe = known.FirstOrDefault(r => r.outputs[0].item.type == ItemType.Tool && r.outputs[0].item.rarity == next);
                var q = new Quest { Id = "hammer", Title = $"{raft.Tier} hammer", Target = QuestTarget.Workbench, Sub = RecipeSub.Hammers,
                    Summary = recipe != null ? recipe.outputs[0].item.displayName : "Make the next hammer" };
                q.Lines.Add(new QuestLine($"{raft.Tier} hammer or better", 0, 1));
                if (recipe != null) AddIngredients(q, recipe, bag);
                list.Add(q);
            }

            // The next ship part (ships have more parts to build, Ed's ask): from 5 ship levels before it opens.
            if (raft.IsShip && !raft.VesselComplete)
            {
                int slot = Enumerable.Range(1, VesselRules.Parts).First(n => !raft.HasVessel(n));
                int opens = EvolutionRules.PartOpensAt(slot);
                if (raft.Level >= opens - 5)
                {
                    string name = VesselRules.PartName(raft.Chosen, slot);
                    var part = known.Select(r => r.outputs[0].item).FirstOrDefault(i => i.type == ItemType.Vessel && i.raftKind == raft.Chosen && i.vesselSlot == slot);
                    var q = new Quest { Id = "vessel", Title = $"{EvolutionRules.LineName(raft.Chosen)}: {name}", Summary = $"Part {slot}/{VesselRules.Parts}, needed at Lv {opens}" };
                    q.Lines.Add(new QuestLine($"Ship Lv {opens}", raft.Level, opens));
                    if (part != null && bag.Count(part) > 0)
                    {
                        q.Target = QuestTarget.Build;
                        q.Lines.Add(new QuestLine($"{name} put on", 0, 1));
                    }
                    else
                    {
                        q.Target = QuestTarget.Workbench;
                        q.Sub = RecipeSub.Vehicle;
                        var recipe = known.FirstOrDefault(r => r.outputs[0].item == part);
                        if (recipe != null) AddIngredients(q, recipe, bag);
                        else q.Lines.Add(new QuestLine($"{name} made", 0, 1));
                    }
                    list.Add(q);
                }
            }
            return list;
        }

        /// <summary>One line per ingredient of <paramref name="r"/>: have/need from the bag.</summary>
        public static void AddIngredients(Quest q, RecipeDefinition r, Inventory bag)
        {
            foreach (var i in r.inputs.Where(x => x.item != null))
                q.Lines.Add(new QuestLine(i.item.displayName, bag.Count(i.item), i.count));
        }
    }
}
