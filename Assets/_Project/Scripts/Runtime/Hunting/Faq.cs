namespace BrineBattle.Hunting
{
    /// <summary>
    /// The full guide, shown only in Settings → FAQ (Ed's call: no guide text on the play screen or in
    /// the other menus). Plain text so the player manual (U-22) can reuse it later.
    /// </summary>
    public static class Faq
    {
        public struct Entry
        {
            public string Question, Answer;
            public Entry(string q, string a) { Question = q; Answer = a; }
        }

        public struct Topic
        {
            public string Name;
            public Entry[] Entries;
            public Topic(string name, params Entry[] entries) { Name = name; Entries = entries; }
        }

        public static readonly Topic[] Topics =
        {
            new Topic("Start",
                new Entry("What do I do in this game?",
                    "You live on a raft on an endless ocean. Monsters and loot float past. Hook them with your harpoon, fight or harvest them, then use the loot to build a bigger, stronger raft, make gear and potions, and raise pets."),
                new Entry("What are the keys?",
                    "Click a floating thing: throw the harpoon at it.\nSpace: reel in.\nB: Bag.  P: Pets.  C: Collection Book.  R: Build.  Esc: Settings, or close a menu.\nYou can also click the picture buttons at the bottom of the screen."),
                new Entry("What do the boxes at the top-left mean?",
                    "Raft: its level and HP. Harpoon: how worn it is (press Fix to mend it). Pets: how many you have and how much room there is.")),

            new Topic("Hunting",
                new Entry("How do I catch something?",
                    "Click a monster or item floating on the water. The harpoon flies out. When it hooks, press Space while the marker is in the green zone to reel it in. A clean reel gives extra loot."),
                new Entry("What do \"Looks dead\", \"Twitching...\" and \"Something moves!\" mean?",
                    "It's a guess about the monster's state, and it can be wrong.\nLooks dead: usually safe to harvest for loot.\nTwitching...: weak; finish it for loot, or tame it as a pet.\nSomething moves!: alive; it may fight you."),
                new Entry("What are Harvest, Finish it, Tame, Fight, Flee and Cut line?",
                    "Harvest: take the loot from a dead monster.\nFinish it: beat a weak monster for its loot.\nTame: try to make a weak, calm monster your pet.\nFight!: start a battle.\nFlee / Cut line: let it go.\nGreen buttons are the safe choice, red is risky, blue lets it go."),
                new Entry("Some monsters are too big. Why?",
                    "A small raft can't fight a big monster. Grow the raft (more parts) to take on bigger ones.")),

            new Topic("Battle",
                new Entry("How do battles work?",
                    "Turns: you, your battle pet and the monster each act by speed. Attack, use a skill or an item. Elements matter: a strong element hits harder."),
                new Entry("What are Manual, Semi-auto and Auto?",
                    "Manual: you pick every action and press Space to reel.\nSemi-auto: basic attacks are automatic; you decide the important turns.\nAuto: the game plays every turn and reels for you.\nChange it in Settings → Game, or during a battle."),
                new Entry("What is auto-use?",
                    "In Semi-auto and Auto, potions are used for you when HP is low. Turn it on or off in Settings → Game."),
                new Entry("What does \"⚠ Danger!\" over a monster mean?",
                    "That monster would probably beat you and your battle pet. Better gear, a stronger pet, or leaving it alone helps."),
                new Entry("What happens if I lose?",
                    "The monster swims off and you get no loot. You rest for 30 seconds before you can hook again, and the waves wash away up to 3 Common materials from your Bag. Gear, pets, raft parts, tools, potions and rarer items are never lost. You start the next fight with full HP.")),

            new Topic("Bag & loot",
                new Entry("Where is my loot?",
                    "In the Bag (B). Use the filter buttons (Materials, Raft, Gear, Potions, Tools) to find things fast."),
                new Entry("What is Salvage and Monster Essence?",
                    "Salvage breaks an item into Monster Essence. Rarer items give more. Essence pays for raft level-ups, gear refining and pet fusing."),
                new Entry("What is auto-salvage?",
                    "Bag → Auto-salvage. Weapons and armor below the tiers you set there are \"misfits\". If auto-salvage is on, misfits are turned into essence when you pick them up. Every other kind of loot is always kept, at any raft level."),
                new Entry("How do I equip gear?",
                    "Bag → Equipment. Press Equip on a weapon or armor. Refine it with essence to make it stronger. Rarer gear has skills.")),

            new Topic("Workbench",
                new Entry("Where do I craft?",
                    "Build (R) → Workbench. Pick a tab: Gear, Potions, Oils, Raft or Dismantle."),
                new Entry("How do I know what I'm missing?",
                    "Every recipe card lists each ingredient as have/need. Green ✔ = enough. Red ✘ = not enough, and it says how many more. Recipes you can make now are at the top, and each tab shows how many are ready, like \"Raft (2)\". Tick \"Show only what I can make now\" to hide the rest."),
                new Entry("What do the tabs make?",
                    "Gear: weapons and armor.\nPotions: heal the hunter, fix the raft, or treat a pet (Kelp Biscuit).\nOils: coat your weapon with an element for a few rounds.\nRaft: modules, habitats, hammers and stronger raft parts (Reinforce).\nDismantle: take crafted things apart to get materials back."),
                new Entry("What does a Mystic pet do here?",
                    "It sometimes makes one extra gear, potion or oil, and it crafts or upgrades your hammer by itself.")),

            new Topic("Raft",
                new Entry("How do I build?",
                    "Build (R) → Build tab. Pick a part, then click a green tile next to the raft. Click a built tile to swap it for the picked part. Use Remove to take a part off; it goes back to the Bag."),
                new Entry("Why can't I build?",
                    "You need a hammer of the raft's level or better. Make or upgrade it in Workbench → Raft. A Mystic pet makes it for you too."),
                new Entry("Why is a part \"too weak\"?",
                    "After a level-up the raft only takes parts of its level or higher. Turn old parts into stronger ones with the Reinforce recipes in Workbench → Raft."),
                new Entry("How do I level up the raft?",
                    "Build → Level up. Every step shows ✔ when done and ✘ when not:\n• beat monsters (5 for Lv 2, 10 for Lv 3, …)\n• monster materials in the Bag (same numbers)\n• Monster Essence of the raft's rarity or better\n• auto-loot modules on half the rows of the left edge\n• a hammer of the raft's level.\nWhen all are ✔, press the green Level up button. Then upgrade your hammer to keep building."),
                new Entry("What are modules?",
                    "Nets and hooks that grab loot floating into them, even monsters (then a battle starts by itself). Put them on the left edge, where drifts come from. They wear out: repair them in Build → Modules, or let a Crew pet do it."),
                new Entry("What are habitats?",
                    "Tide Pool and Sky Perch. Each one on the raft gives room for one more pet.")),

            new Topic("Pets",
                new Entry("How do I get a pet?",
                    "Hook a monster that is \"Twitching...\" and calm, then press Tame. Rarer monsters are harder to tame. You need room: build habitats for more."),
                new Entry("What are Familiar, Crew and Mystic?",
                    "Familiar: fights with you (pick one with \"Take into battle\").\nCrew: repairs the raft, harpoon and modules, and can build parts.\nMystic: helps craft and makes hammers.\nSet their chores in Pets → Chores."),
                new Entry("How do pets grow?",
                    "Pets → pick a pet → Feed. Monster loot adds growth (its favourite element counts double). When the growth bar is full it evolves: Young → Adult → Elder, and gets stronger. Potions add stats, a few per stage."),
                new Entry("Do pets get old?",
                    "Yes. Each beaten monster makes every pet one battle older. The Life bar shows how many battles are left. When it runs out the pet passes away, leaves essence, and is remembered in the Book."),
                new Entry("What is Fuse?",
                    "Pets → Fuse puts one pet into another. Same kind: the pet becomes rarer. Different kinds: a hybrid. The used pet is gone, and it can fail. Essence as a catalyst makes it safer."),
                new Entry("What is Look?",
                    "A costume: Plain, Scarf, Bandana or Crown. It's just for looks.")),

            new Topic("Book & saving",
                new Entry("What is the Collection Book?",
                    "C opens it. Monsters, items and pets you've found. ??? means not found yet. Each tab shows found/total."),
                new Entry("How do I save?",
                    "Settings → Save → Save now. There is one save file. In Semi-auto and Auto it also saves by itself every 5 minutes once a save exists. The top-right corner shows when."),
                new Entry("What if I fall asleep while playing?",
                    "If you don't press anything for 10 minutes, the game pauses by itself and shows \"You fell asleep\". Nothing fights, gets hooked or gets lost while you're away. In Semi-auto and Auto it saves first. A battle that is already going finishes first. If you lose a battle while away (no input for a minute), it pauses right away, without waiting for the timer. A loss costs a short rest and a few Common materials (see Battle). Press any key or click to continue. Change the time (or turn it off) in Settings → Game → Pause when I'm away."),
                new Entry("How do I start over?",
                    "Settings → Save → Save and go to the main menu, then New game. Your old save is kept as a backup.")),
        };
    }
}
