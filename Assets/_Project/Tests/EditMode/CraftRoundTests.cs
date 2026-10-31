using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's raft/workbench batch: accessories, planks-only walking, timed Crew installs.</summary>
    public class CraftRoundTests
    {
        static ItemDefinition Item(ItemType t, Rarity r = Rarity.Common, int power = 0, Element e = Element.None)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.type = t;
            i.rarity = r;
            i.gearPower = power;
            i.element = e;
            i.displayName = t.ToString();
            return i;
        }

        [Test]
        public void AnAccessoryAddsAttackDefenseElementAndSkills()
        {
            var inv = new Inventory();
            var charm = Item(ItemType.Accessory, Rarity.Rare, 5, Element.Fire);
            charm.passives = new[] { GearPassive.Swift };
            inv.Add(charm, 1);
            var gear = new Equipment();
            Assert.IsTrue(gear.Equip(charm, inv, Rarity.Common));
            Assert.AreEqual(GearSlot.Accessory, GearRules.SlotOf(ItemType.Accessory));
            Assert.AreEqual(3, gear.Attack);
            Assert.AreEqual(2, gear.Defense);
            Assert.AreEqual(Element.Fire, gear.Element, "a neutral weapon lets the accessory's element count");
            Assert.AreEqual(6 + GearSkillRules.SwiftSpeed, Battle.Combatant.Hunter(gear).Speed);
        }

        [Test]
        public void OnlyPlanksAreWalkable()
        {
            var g = new RaftGrid();
            g.FillSquare(Item(ItemType.Raft), 1);
            Assert.IsTrue(g.Walkable(new Cell(0, 0)));
            Assert.IsFalse(g.Walkable(new Cell(5, 5)));
            var net = Item(ItemType.Module);
            g.Restore(new[]
            {
                new System.Collections.Generic.KeyValuePair<Cell, ItemDefinition>(new Cell(0, 0), Item(ItemType.Raft)),
                new System.Collections.Generic.KeyValuePair<Cell, ItemDefinition>(new Cell(1, 0), net),
            }, Rarity.Common);
            Assert.IsFalse(g.Walkable(new Cell(1, 0)), "nobody stands on an auto-loot module");
        }

        [Test]
        public void ACrewInstallChecksTheSpotAgainWhenItFinishes()
        {
            var g = new RaftGrid();
            var plank = Item(ItemType.Raft);
            g.FillSquare(plank, 1);
            var inv = new Inventory();
            inv.Add(plank, 2);
            Assert.IsTrue(CompanionWork.CrewPick(g, inv, out var cell, out var part));
            Assert.IsTrue(CompanionWork.CrewInstall(g, inv, cell, part));
            Assert.AreEqual(1, inv.Count(plank));
            Assert.IsFalse(CompanionWork.CrewInstall(g, inv, cell, part), "the spot is taken now");
            Assert.AreEqual(1, inv.Count(plank), "nothing spent on a failed install");
            Assert.Greater(CompanionWork.InstallSeconds, 0f);
        }
    }
}
