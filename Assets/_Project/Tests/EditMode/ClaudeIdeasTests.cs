using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Encounters;
using BrineBattle.Items;
using BrineBattle.Rafts;
using BrineBattle.Saving;
using BrineBattle.World;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Claude's ideas built on 2026-10-31: C-14 reel styles, C-15 firsts, C-16 favourite food, C-18 layout bonuses, C-19 lantern.</summary>
    public class ClaudeIdeasTests
    {
        static ItemDefinition Item(ItemType type, Rarity rarity = Rarity.Common)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{rarity} {type}";
            i.id = $"{rarity}_{type}".ToLowerInvariant();
            i.type = type;
            i.rarity = rarity;
            return i;
        }

        // ---- C-14 ----

        [Test]
        public void CatchStyleFollowsStateSizeAndElement()
        {
            Assert.AreEqual(CatchStyle.Limp, ReelGame.StyleOf(Element.Water, DriftState.Dead, 1));
            Assert.AreEqual(CatchStyle.Sinker, ReelGame.StyleOf(Element.Fire, DriftState.Alive, 4));
            Assert.AreEqual(CatchStyle.ZigZag, ReelGame.StyleOf(Element.Water, DriftState.Alive, 1));
            Assert.AreEqual(CatchStyle.Jumper, ReelGame.StyleOf(Element.Earth, DriftState.Weakened, 2));
            Assert.AreEqual(CatchStyle.Darter, ReelGame.StyleOf(Element.Wind, DriftState.Alive, 1));
        }

        [Test]
        public void LimpCatchMovesLessThanALiveOne()
        {
            float Travel(CatchStyle s)
            {
                var g = new ReelGame(0.3f, 0.5f, BattleMode.Auto, new System.Random(4), s);
                float last = g.Catch, sum = 0f;
                for (int i = 0; i < 120; i++) { g.Step(1f / 60f, false, false); sum += Mathf.Abs(g.Catch - last); last = g.Catch; }
                return sum;
            }
            Assert.Less(Travel(CatchStyle.Limp), Travel(CatchStyle.ZigZag));
        }

        // ---- C-15 ----

        [Test]
        public void FirstsPayOnceAndRestore()
        {
            var f = new Firsts();
            Assert.IsTrue(f.Mark("boss"));
            Assert.IsFalse(f.Mark("boss"), "only the first time");
            Assert.IsFalse(f.Mark("nonsense"));
            var g = new Firsts();
            g.Restore(f.Done.Concat(new[] { "nonsense" }));
            Assert.IsTrue(g.Has("boss"));
            Assert.AreEqual(1, g.Count);
        }

        // ---- C-16 ----

        [Test]
        public void FavouriteFoodDoublesExpAndCheersUp()
        {
            var c = new Companion { Id = "eel", Name = "Eel", Element = Element.Water, MaxHp = 20, Hp = 20 };
            c.CatchUp();
            var gear = Item(ItemType.Weapon);
            int plain = CompanionGrowth.FeedPoints(gear, c);
            c.Favourite = (int)FoodKind.Gear;
            Assert.AreEqual(plain * 2, CompanionGrowth.FeedPoints(gear, c));
            Assert.AreEqual(CompanionGrowth.FeedPoints(Item(ItemType.Material), c), MinionRules.ExpFor(Item(ItemType.Material), c), "only the favourite kind doubles");
            c.SinceFavourite = MinionRules.SulkSeconds + 1f;
            Assert.IsTrue(c.Sulking);
            Assert.AreEqual(0.8f, MinionRules.WorkSpeed(c), 1e-5f);
            CompanionGrowth.Feed(c, gear);
            Assert.IsFalse(c.Sulking);
        }

        [Test]
        public void FavouriteIsSavedAndOldSavesGetOneFromTheId()
        {
            var p = new SaveData.Pet { id = "eel", name = "Eel", level = 1, maxHp = 10, favourite = 2 };
            Assert.AreEqual(2, SaveGame.LoadPet(p).Favourite);
            p.favourite = -1;
            int a = SaveGame.LoadPet(p).Favourite;
            Assert.That(a, Is.InRange(0, 3));
            Assert.AreEqual(a, SaveGame.LoadPet(p).Favourite, "same id, same favourite");
        }

        // ---- C-18 ----

        [Test]
        public void ThreeModulesInALineReachFurther()
        {
            var m = Item(ItemType.Module);
            var parts = new Dictionary<Cell, ItemDefinition> { [new Cell(0, 0)] = m, [new Cell(0, 1)] = m };
            Assert.AreEqual(0f, SetBonusRules.ModuleReach(parts, new Cell(0, 0)));
            parts[new Cell(0, 2)] = m;
            Assert.AreEqual(SetBonusRules.LineReach, SetBonusRules.ModuleReach(parts, new Cell(0, 0)));
            Assert.AreEqual(3, SetBonusRules.LineThrough(parts, new Cell(0, 1)));
        }

        [Test]
        public void HabitatWithPlanksAllRoundIsCozy()
        {
            var plank = Item(ItemType.Raft);
            var parts = new Dictionary<Cell, ItemDefinition> { [new Cell(0, 0)] = Item(ItemType.Habitat) };
            foreach (var n in new Cell(0, 0).Neighbours().Take(3)) parts[n] = plank;
            Assert.IsFalse(SetBonusRules.Cozy(parts, new Cell(0, 0)));
            Assert.AreEqual(1, SetBonusRules.HealingHabitats(parts));
            foreach (var n in new Cell(0, 0).Neighbours()) parts[n] = plank;
            Assert.IsTrue(SetBonusRules.Cozy(parts, new Cell(0, 0)));
            Assert.AreEqual(2, SetBonusRules.HealingHabitats(parts));
        }

        // ---- C-19 ----

        [Test]
        public void LanternHelpsAtNightOnlyAndMoonCreaturesGlow()
        {
            Assert.AreEqual(WorldRules.SightRange(Weather.Clear, TimeOfDay.Night) + WorldRules.LanternReach, WorldRules.SightRange(Weather.Clear, TimeOfDay.Night, true));
            Assert.AreEqual(WorldRules.SightRange(Weather.Fog, TimeOfDay.Day), WorldRules.SightRange(Weather.Fog, TimeOfDay.Day, true));
            Assert.IsTrue(WorldRules.Glows(Element.Water, TimeOfDay.Night));
            Assert.IsFalse(WorldRules.Glows(Element.Water, TimeOfDay.Day));
            Assert.IsFalse(WorldRules.Glows(Element.Fire, TimeOfDay.Night));
        }
    }
}
