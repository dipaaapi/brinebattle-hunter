using BrineBattle.Battle;
using BrineBattle.Items;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class HazardTests
    {
        static Combatant Mon(int atk = 10, StatusEffect s = StatusEffect.None) =>
            new Combatant { Name = "Eel", MaxHp = 30, Hp = 30, Attack = atk, Inflicts = s, InflictChance = 1f };

        [Test]
        public void AggressiveMonstersLashOutMoreOften()
        {
            Assert.Greater(HazardRules.AmbushChance(1f), HazardRules.AmbushChance(0f));
            Assert.Greater(HazardRules.OutburstChance(0.5f, true), HazardRules.OutburstChance(0.5f, false));
        }

        [Test]
        public void AmbushHitsHunterOrPetAndCanGiveAnAilment()
        {
            var rng = new System.Random(4);
            int hunter = 0, pet = 0, ailing = 0;
            for (int i = 0; i < 400; i++)
            {
                var h = HazardRules.Ambush(Mon(s: StatusEffect.Poison), Combatant.Hunter(), new Combatant { MaxHp = 10, Hp = 10 }, 1f, rng);
                if (h.Target == HazardTarget.Hunter) hunter++;
                if (h.Target == HazardTarget.Pet) pet++;
                if (h.Status == StatusEffect.Poison) ailing++;
            }
            Assert.Greater(hunter, 0);
            Assert.Greater(pet, 0);
            Assert.Greater(ailing, 0);
        }

        [Test]
        public void NoPetMeansOnlyTheHunterIsHit()
        {
            var rng = new System.Random(1);
            for (int i = 0; i < 100; i++)
                Assert.AreNotEqual(HazardTarget.Pet, HazardRules.Outburst(Mon(), Combatant.Hunter(), null, 1f, true, rng).Target);
        }

        [Test]
        public void HazardNeverKnocksOutTheHunterButCanAPet()
        {
            var hunter = new Combatant { MaxHp = 60, Hp = 3 };
            HazardRules.Apply(new Hazard { Target = HazardTarget.Hunter, Damage = 50 }, hunter);
            Assert.AreEqual(1, hunter.Hp);
            var pet = new Combatant { MaxHp = 10, Hp = 3 };
            HazardRules.Apply(new Hazard { Target = HazardTarget.Pet, Damage = 50, Status = StatusEffect.Bleed }, pet);
            Assert.AreEqual(0, pet.Hp);
            Assert.AreEqual(StatusEffect.Bleed, pet.Status);
        }

        [Test]
        public void HabitatsMakePetsHealFaster()
        {
            Assert.Less(HazardRules.PetRegenSeconds(2), HazardRules.PetRegenSeconds(0));
        }

        [Test]
        public void TreatHealsTheRightOneAndCures()
        {
            var tonic = ScriptableObject.CreateInstance<ItemDefinition>();
            tonic.effect = ConsumableEffect.HealHunter; tonic.effectAmount = 25;
            var biscuit = ScriptableObject.CreateInstance<ItemDefinition>();
            biscuit.effect = ConsumableEffect.HealCompanion; biscuit.effectAmount = 20;
            var hunter = new Combatant { MaxHp = 60, Hp = 10 };
            Assert.IsTrue(HazardRules.Treat(tonic, hunter, false));
            Assert.AreEqual(35, hunter.Hp);
            Assert.IsFalse(HazardRules.Treat(biscuit, hunter, false), "a pet treat isn't for the hunter");
            var pet = new Combatant { MaxHp = 30, Hp = 0, Status = StatusEffect.Poison, StatusRounds = 3 };
            Assert.IsTrue(HazardRules.Treat(biscuit, pet, true));
            Assert.AreEqual(20, pet.Hp);
            var antidote = ScriptableObject.CreateInstance<ItemDefinition>();
            antidote.effect = ConsumableEffect.CureStatus; antidote.cures = StatusEffect.Poison;
            Assert.IsTrue(HazardRules.Treat(antidote, pet, true));
            Assert.AreEqual(StatusEffect.None, pet.Status);
            Assert.IsFalse(HazardRules.Treat(antidote, pet, true), "nothing left to cure");
        }
    }
}
