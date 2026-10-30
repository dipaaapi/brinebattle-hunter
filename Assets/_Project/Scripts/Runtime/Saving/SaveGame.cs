using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;
using BrineBattle.Rafts;

namespace BrineBattle.Saving
{
    /// <summary>
    /// Everything a save file holds. Plain serializable fields (ids and ints) so Unity's JsonUtility
    /// can write it and saves survive asset renames as long as item ids stay the same.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = SaveGame.Version;
        public string savedAt;
        public List<Stack> items = new List<Stack>();
        public List<EssenceStack> essence = new List<EssenceStack>();
        public string weapon;
        public string armor;
        public List<Stack> refines = new List<Stack>();
        public int harpoon = -1;
        public int raftTier;
        /// <summary>Monsters killed since the last raft tier upgrade.</summary>
        public int raftKills;
        /// <summary>Alchemy Mastery points (Idea Round 3). Older saves have none and start at their raft level.</summary>
        public bool hasMastery;
        public int mastery;
        public List<Part> parts = new List<Part>();
        public List<Part> modules = new List<Part>();
        public List<Pet> companions = new List<Pet>();
        public int activeCompanion = -1;
        /// <summary>Companions that died of old age (Book).</summary>
        public List<Grave> memorials = new List<Grave>();
        /// <summary>The hunter's HP and ailment between battles (U-32); -1 = full (older saves).</summary>
        public int hunterHp = -1, hunterStatus, hunterStatusRounds;
        /// <summary>Raft type picked at each raft level-up (U-31), from Lv 2 up.</summary>
        public List<int> raftPaths = new List<int>();
        /// <summary>Monsters that ate raft parts (Phase 8).</summary>
        public List<Gorge> gorged = new List<Gorge>();
        public int habitats;
        public string collection;
        /// <summary>Menu settings (Settings, Bag, Build); false in saves made before they were saved.</summary>
        public bool hasSettings;
        public Settings settings = new Settings();

        /// <summary>Choices made in the menus, so a loaded save plays the way it was left.</summary>
        [Serializable]
        public class Settings
        {
            public int battleMode;
            public bool autoItems = true;
            public bool autoSalvage;
            public int weaponTier, armorTier, companionTier;
            public bool crewBuilds = true;
            public bool mysticRefines = true;
            public int mysticKeep = 2;
            /// <summary>UI size (Settings); 0 in older saves means "leave as is".</summary>
            public float uiScale;
            /// <summary>Per module kind: the durability share at which the Crew repairs it.</summary>
            public List<Share> crewRepairAt = new List<Share>();
        }
        [Serializable] public class Share { public string id; public float share; }

        [Serializable] public class Stack { public string id; public int count; }
        [Serializable] public class EssenceStack { public int rarity; public int element; public int count; }
        /// <summary>A raft cell. For modules, count is the durability left.</summary>
        [Serializable] public class Part { public int x; public int z; public string id; public int count; }
        [Serializable]
        public class Pet
        {
            public string id, name;
            public int element, habitat, rarity, role, maxHp, attack, defense, speed;
            /// <summary>Battles won since taming and growth stage; 0 in older saves (a young companion).</summary>
            public int age, stage;
            /// <summary>Feeding points, potions infused, the fusion echo and the look (0 / null in older saves).</summary>
            public int growth, infusions, look;
            public string echo;
            /// <summary>HP now and ailment (U-32/U-33); -1 in older saves = full HP.</summary>
            public int hp = -1, status, statusRounds;
        }
        [Serializable] public class Gorge { public string monster; public List<string> parts = new List<string>(); }
        [Serializable] public class Grave { public string id, name; public int rarity, role, age; }
    }

    /// <summary>Turns game state into <see cref="SaveData"/> and back (GDD: save points). Plain C# for EditMode tests.</summary>
    public static class SaveGame
    {
        public const int Version = 2;

        public static SaveData Capture(Inventory bag, Equipment gear, Durability harpoon, RaftGrid raft,
            IEnumerable<KeyValuePair<Cell, int>> moduleDurability, CompanionRoster pets, string collection, DateTime now)
        {
            var d = new SaveData { savedAt = now.ToString("yyyy-MM-dd HH:mm"), collection = collection };
            foreach (var kv in bag.Items) d.items.Add(new SaveData.Stack { id = kv.Key.id, count = kv.Value });
            foreach (var kv in bag.Essence)
                d.essence.Add(new SaveData.EssenceStack { rarity = (int)kv.Key.Rarity, element = (int)kv.Key.Element, count = kv.Value });
            d.weapon = gear.Get(GearSlot.Weapon)?.id;
            d.armor = gear.Get(GearSlot.Armor)?.id;
            foreach (var kv in gear.Refines) d.refines.Add(new SaveData.Stack { id = kv.Key.id, count = kv.Value });
            d.harpoon = harpoon?.Current ?? -1;
            d.raftTier = (int)raft.Tier;
            d.raftKills = raft.Kills;
            d.raftPaths = raft.Paths.Select(p => (int)p).ToList();
            foreach (var kv in raft.Parts) d.parts.Add(new SaveData.Part { x = kv.Key.X, z = kv.Key.Z, id = kv.Value.id });
            if (moduleDurability != null)
                foreach (var kv in moduleDurability) d.modules.Add(new SaveData.Part { x = kv.Key.X, z = kv.Key.Z, count = kv.Value });
            foreach (var c in pets.All)
                d.companions.Add(new SaveData.Pet
                {
                    id = c.Id, name = c.Name, element = (int)c.Element, habitat = (int)c.Habitat, rarity = (int)c.Rarity,
                    role = (int)c.Role, maxHp = c.MaxHp, attack = c.Attack, defense = c.Defense, speed = c.Speed,
                    age = c.Age, stage = (int)c.Stage, growth = c.Growth, infusions = c.Infusions, look = c.Look,
                    echo = string.IsNullOrEmpty(c.Echo) ? null : c.Echo,
                    hp = c.Hp, status = (int)c.Status, statusRounds = c.StatusRounds,
                });
            foreach (var m in pets.Memorials)
                d.memorials.Add(new SaveData.Grave { id = m.Id, name = m.Name, rarity = (int)m.Rarity, role = (int)m.Role, age = m.Age });
            d.activeCompanion = pets.Active == null ? -1 : pets.All.ToList().IndexOf(pets.Active);
            d.habitats = pets.Habitats;
            return d;
        }

        /// <summary>
        /// Loads <paramref name="d"/> into the given objects. Items are found by id through <paramref name="find"/>;
        /// unknown ids are skipped and listed in the returned warnings. Module durability is returned per
        /// cell for the caller to apply once the modules exist.
        /// </summary>
        public static List<string> Apply(SaveData d, Func<string, ItemDefinition> find, Inventory bag, Equipment gear,
            Durability harpoon, RaftGrid raft, CompanionRoster pets, out Dictionary<Cell, int> moduleDurability)
        {
            var missing = new List<string>();
            ItemDefinition Find(string id)
            {
                if (string.IsNullOrEmpty(id)) return null;
                var item = find(id);
                if (item == null && !missing.Contains(id)) missing.Add(id);
                return item;
            }

            bag.Clear();
            foreach (var s in d.items) bag.Add(Find(s.id), s.count);
            foreach (var e in d.essence) bag.AddEssence(new EssenceKey((Rarity)e.rarity, (Element)e.element), e.count);
            gear.Restore(Find(d.weapon), Find(d.armor),
                d.refines.Select(r => new KeyValuePair<ItemDefinition, int>(Find(r.id), r.count)));
            if (harpoon != null && d.harpoon >= 0) harpoon.Set(d.harpoon);
            if (d.parts.Count > 0)
                raft.Restore(d.parts.Select(p => new KeyValuePair<Cell, ItemDefinition>(new Cell(p.x, p.z), Find(p.id))), (Rarity)d.raftTier);
            raft.Kills = d.raftKills;
            raft.RestorePaths((d.raftPaths ?? new List<int>()).Select(p => (RaftKind)p));
            moduleDurability = d.modules.ToDictionary(m => new Cell(m.x, m.z), m => m.count);
            pets.Restore(d.companions.Select(p => new Companion
            {
                Id = p.id, Name = p.name, Element = (Element)p.element, Habitat = (Habitat)p.habitat, Rarity = (Rarity)p.rarity,
                Role = (CompanionRole)p.role, MaxHp = p.maxHp, Attack = p.attack, Defense = p.defense, Speed = p.speed,
                Age = p.age, Stage = (GrowthStage)p.stage, Growth = p.growth, Infusions = p.infusions, Look = p.look,
                Echo = string.IsNullOrEmpty(p.echo) ? null : p.echo,
                Hp = p.hp < 0 ? p.maxHp : Math.Min(p.hp, p.maxHp),
                Status = (Battle.StatusEffect)p.status, StatusRounds = p.statusRounds,
            }), d.activeCompanion, d.habitats);
            pets.RestoreMemorials(d.memorials.Select(m => new Memorial
                { Id = m.id, Name = m.name, Rarity = (Rarity)m.rarity, Role = (CompanionRole)m.role, Age = m.age }));
            return missing;
        }
    }
}
