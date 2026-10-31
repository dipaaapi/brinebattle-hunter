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
            int level = (int)raft.Tier + 1;

            if (raft.CanChoose)
                list.Add(new Quest { Id = "type", Title = "Pick the raft type", Summary = "Fortress, Sky or Beast", Target = QuestTarget.RaftTree,
                    Lines = { new QuestLine("Raft type picked", 0, 1) } });

            if (raft.Tier < Rarity.Mythic)
            {
                var q = new Quest { Id = "level", Title = $"Raft Lv {level + 1}", Target = QuestTarget.RaftTree };
                q.Lines.Add(new QuestLine("Monsters beaten", raft.Kills, RaftRules.KillsToUpgrade(raft.Tier)));
                q.Lines.Add(new QuestLine("Monster materials", bag.MaterialCount(), RaftRules.MaterialsToUpgrade(raft.Tier)));
                q.Lines.Add(new QuestLine($"{raft.Tier}+ essence", bag.EssenceAtLeast(raft.Tier), RaftRules.UpgradeCost(raft.Tier)));
                q.Lines.Add(new QuestLine("Modules on the left edge", raft.UpstreamModules().guarded, raft.ModulesNeeded));
                q.Lines.Add(new QuestLine($"{raft.Tier} hammer or better", RaftRules.HasHammer(bag, raft.Tier) ? 1 : 0, 1));
                if (raft.BossesNeeded) q.Lines.Add(new QuestLine($"Lv {level} boss beaten", raft.BossBeaten(raft.Tier) ? 1 : 0, 1));
                q.Summary = q.Done ? "Ready: level up in the Raft tree" : $"{q.Missing} left";
                list.Add(q);
            }

            if (raft.BossesNeeded && !raft.BossBeaten(raft.Tier))
                list.Add(new Quest { Id = "boss", Title = $"Beat the Lv {level} boss", Target = QuestTarget.Boss,
                    Summary = "Once the level's monsters are beaten it shows up on its own, or summon it from here when you're ready",
                    Lines = { new QuestLine("Monsters beaten", raft.Kills, RaftRules.KillsToUpgrade(raft.Tier)), new QuestLine("Boss beaten", 0, 1) } });

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

            if (raft.Chosen != RaftKind.Plain && !raft.VesselComplete)
            {
                int slot = Enumerable.Range(1, VesselRules.Parts).First(s => !raft.HasVessel(s));
                string name = VesselRules.PartName(raft.Chosen, slot);
                var part = known.Select(r => r.outputs[0].item).FirstOrDefault(i => i.type == ItemType.Vessel && i.raftKind == raft.Chosen && i.vesselSlot == slot);
                var q = new Quest { Id = "vessel", Title = $"{VesselRules.Vehicle(raft.Chosen)}: {name}", Summary = $"Part {slot}/{VesselRules.Parts}" };
                q.Lines.Add(new QuestLine($"Raft Lv {VesselRules.UnlockLevel(slot)}", level, VesselRules.UnlockLevel(slot)));
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

            if (raft.Chosen != RaftKind.Plain)
            {
                var q = new Quest { Id = "leave", Title = $"Leave the sea", Target = QuestTarget.Leave, Summary = VesselRules.LeaveVerb(raft.Chosen) };
                q.Lines.Add(new QuestLine("Raft Lv 6", level, 6));
                q.Lines.Add(new QuestLine("Vehicle parts on", raft.VesselCount, VesselRules.Parts));
                q.Lines.Add(new QuestLine("Lv 6 boss beaten", raft.BossBeaten(Rarity.Mythic) ? 1 : 0, 1));
                list.Add(q);
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
