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
                    "Click a floating thing: throw the harpoon at it.\nSpace: reel in.\nB: Bag.  P: Pets.  C: Collection Book.  R: Build.  Esc: Settings, or close a menu.\nMouse wheel or + / -: zoom in and out.  Right-drag or Q / E: turn the view.  F: put the view back.\nYou can also click the picture buttons at the bottom of the screen."),
                new Entry("What do the boxes at the top-left mean?",
                    "Hunter: your HP and any ailment. Raft: its level, HP, size and type. Harpoon: how worn it is (press Fix to mend it). Pets: how many you have and how much room there is. The line under them shows the day, the time, the weather and the forecast (what weather comes next, and when).")),

            new Topic("Hunting",
                new Entry("How do I catch something?",
                    "Click a monster or item floating on the water. The harpoon flies out. When it hooks, press Space while the marker is in the green zone to reel it in. A clean reel gives extra loot."),
                new Entry("What do \"Looks dead\", \"Twitching...\" and \"Something moves!\" mean?",
                    "It's a guess about the monster's state, and it can be wrong.\nLooks dead: usually safe to harvest for loot.\nTwitching...: weak; finish it for loot, or tame it as a pet.\nSomething moves!: alive; it may fight you."),
                new Entry("What are Harvest, Finish it, Tame, Fight, Flee and Cut line?",
                    "Harvest: take the loot from a dead monster.\nFinish it: beat a weak monster for its loot.\nTame: try to make a weak, calm monster your pet.\nFight!: start a battle.\nCut line: let it go, nothing lost.\nFlee: run from it, but it bites off one raft part on the way (see Raft).\nGreen buttons are the safe choice, red is risky, blue lets it go."),
                new Entry("What is floating loot?",
                    "Crates, bundles and wrecks that float past (\"Floating loot\" over them). Hook one to get raft materials like planks. Raft modules catch floating loot of their own rarity or lower, and a Familiar pet can fetch it too."),
                new Entry("Can a monster hurt me outside a battle?",
                    "Yes. A live monster may lash out while you reel it in, and a monster you just beat may thrash one last time. Either can hit you or a pet, and may leave an ailment. It never knocks you out, but it can knock out a pet."),
                new Entry("Some monsters are too big. Why?",
                    "A small raft can't fight a big monster. Grow the raft (more parts) to take on bigger ones.")),

            new Topic("Day & weather",
                new Entry("Is there day and night?",
                    "Yes. A full day takes 8 minutes: dawn, day, dusk and night. Time stops while a menu is open or a battle is on."),
                new Entry("What are Sun and Moon creatures?",
                    "Fire, magma, sand and steam creatures are Sun creatures: by day they hit 20% harder and take a bit less damage, at night they hit 10% softer. Water, ice and mud creatures are Moon creatures, the other way round. Earth, wind and lightning are neither. At dawn and dusk nobody is boosted.\nThis counts for monsters, your pets, and you: gear with a sun or moon element powers you up at its time. Sun creatures turn up more by day, Moon creatures more at night."),
                new Entry("What changes at night?",
                    "More drifts are alive, hints are wrong a bit more often, and you can only read the names of drifts close to the raft. Far ones show as \"???\"."),
                new Entry("What does each weather do?",
                    "Clear: calm, and on clear days more drifts are dead (easy loot).\nRain: water creatures hit harder, hints a bit worse.\nRough sea: big waves, drifts float by faster, your harpoon throw is 2 m shorter and the reel's sweet spot is smaller. More creatures are alive.\nStorm: all of that, worse. Lightning creatures hit harder, lots of floating loot, and lightning can strike the raft.\nFog: flat sea, but you can only read drifts close by and hints are often wrong."),
                new Entry("Lightning hit my raft. What do I do?",
                    "A part is on fire. Click the fire twice to throw water on it. You can't hook anything until it's out. If it burns for 15 seconds, that part is lost. A Crew pet puts fires out by itself after 5 seconds. Fortress rafts get hit half as often; Sky rafts stand tall and get hit more."),
                new Entry("What is the forecast for?",
                    "It tells you the next weather and when it comes. A storm only grows out of rain or a rough sea, so you get a warning: heal up, get a Crew pet, or reel in what you can before it hits."),
                new Entry("Why is there so much floating loot after a storm?",
                    "When a storm clears, the next 3 drifts are wreckage: floating loot one rarity higher than usual.")),
            new Topic("Battle",
                new Entry("How do battles work?",
                    "Turns: you, your battle pet and the monster each act by speed. Attack, use a skill or an item. Elements matter: a strong element hits harder."),
                new Entry("What are Manual, Semi-auto and Auto?",
                    "Manual: you pick every action and press Space to reel.\nSemi-auto: basic attacks are automatic; you decide the important turns.\nAuto: the game plays every turn and reels for you.\nChange it in Settings → Game, or during a battle."),
                new Entry("What is auto-use?",
                    "In Semi-auto and Auto, potions are used for you when HP is low. Turn it on or off in Settings → Game."),
                new Entry("What are ailments?",
                    "Some monsters' hits leave an ailment. The sign over a drift tells you which one:\n☠ Poison: lose a little HP every round.\n♦ Bleed: lose more HP every round, but it ends sooner.\n? Confusion: sometimes you hit yourself.\n◉ Blind: you often miss and can't use gear skills.\nAilments stay after a battle and keep ticking every few seconds, but outside battle they never take your last HP."),
                new Entry("How do I cure ailments and heal?",
                    "Workbench → Potions: Toxic Relief (poison), Clotting Patch (bleeding), Confusion Relief, Eye Cleanser (blind), Brine Tonic and Big Brine Tonic (HP), Kelp Biscuit (pet HP). In battle use them from the item buttons (Semi-auto and Auto use them for you). On the raft: Bag → Use for yourself, Pets → pick a pet for your pets. You and your pets also heal slowly on the raft."),
                new Entry("What does \"⚠ Danger!\" over a monster mean?",
                    "That monster would probably beat you and your battle pet. Better gear, a stronger pet, or leaving it alone helps."),
                new Entry("What happens if I lose?",
                    "The monster swims off and you get no loot. It also eats one raft part from the edge of your raft (see Raft → Gorged monsters). You rest for 30 seconds before you can hook again, and the waves wash away up to 3 Common materials from your Bag. Gear, pets, tools, potions and rarer items in the Bag are never lost. Your pets hide in the storage box until you're back up. After the rest you're back to full HP with no ailments. Losing a fight a module started doesn't cost a raft part.")),

            new Topic("Bag & loot",
                new Entry("Where is my loot?",
                    "In the Bag (B). Use the filter buttons (Materials, Raft, Gear, Potions, Tools) to find things fast."),
                new Entry("What is Salvage and Monster Essence?",
                    "Salvage breaks an item into Monster Essence. Rarer items give more. Essence pays for raft level-ups, gear refining and pet fusing."),
                new Entry("What is auto-salvage?",
                    "Bag → Auto-salvage. Weapons and armor below the tiers you set there are \"misfits\". If auto-salvage is on, misfits are turned into essence when you pick them up. Every other kind of loot is always kept, at any raft level."),
                new Entry("How do I equip gear?",
                    "Bag → Equipment. Press Equip on a weapon or armor. Refine it with essence to make it stronger. Rarer gear has skills.")),

            new Topic("Mastery",
                new Entry("What is Alchemy Mastery?",
                    "Your skill as a monster alchemist, shown at the top-left. Harvesting or beating a monster gives 2 points, taming 3, crafting 1. Each level needs more points (10, 20, 30 …)."),
                new Entry("What does Mastery do?",
                    "• Hints over drifts (\"Looks dead\" etc.) are right more often: 60% at Lv 1, up to 96%.\n• New recipes appear: Uncommon things at Lv 2, Rare at Lv 3, Epic at Lv 4, Legendary at Lv 5, Mythic at Lv 6. Until then they show as ??? in the Workbench and the Book.\n• From Lv 3 you can combine 5 essence into 1 of the next rarity (Bag → Essence)."),
                new Entry("Why is a recipe \"???\"?",
                    "Your Alchemy Mastery isn't high enough yet. The card says which level reveals it. Hunt, harvest and craft to level up. Book → Recipes lists every recipe you know.")),

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
                new Entry("Which module should I build?",
                    "Each module catches floating loot up to its own rarity: Drift Net and Hook Arm (Common), Tarred Net (Uncommon), Shell Trap (Rare), Coral Snare (Epic), Leviathan Grapple (Legendary), Abyssal Maw (Mythic). Each one upgrades from the one before in Workbench → Raft. Monsters are caught by any module."),
                new Entry("What are raft types?",
                    "The raft parts you build with decide the raft's type:\n• Fortress (Shell Plating, Shellback Plate, Coralbound Plank): +40% raft HP, +3 DEF, −1 SPD.\n• Sky (Wing Sail): +3 SPD, +4 m hook reach, −10% raft HP.\n• Beast (Beast Harness): +4 ATK, +1 SPD.\nA kind sets the type when at least 3 parts and half of all raft parts are that kind. Two kinds with 3+ parts and almost a third each make a Hybrid with half of both bonuses. Otherwise the raft is Plain. Build shows the type and how many parts of each kind you have. Modules and habitats don't count."),
                new Entry("How do I make typed parts?",
                    "Workbench → Raft. Lv 1 of each kind: 1 plank + 2 of its material (Eel Scale, Gale Feather or Eel Fang). Higher levels come from the raft tree: first make that level's tree material (for example Hardened Scale), then turn your last part + that material into the next level's part."),
                new Entry("What is the raft tree?",
                    "Every time you level up the raft you pick a type to build toward: Fortress, Sky or Beast. That opens its branch up to the new level: its tree materials and parts. Build → Tree shows every level and what is open (✔) or locked (🔒). Picking another type later opens its branch up to that level, so you can always switch. Locked recipes show which pick they need."),
                new Entry("What is a gorged monster?",
                    "When you lose a fight or flee, the monster eats one raft part from the edge (never one that would split the raft, and never when the raft has 5 parts or fewer). It swims off gorged: later it comes back with \"Gorged!\" over it, 20% stronger for each part it ate. Beat it to get every part back in your Bag, plus one extra loot roll per part. Build shows how many parts gorged monsters are holding."),
                new Entry("What are habitats?",
                    "Tide Pool and Sky Perch. Each one on the raft gives room for one more pet, and makes pets heal faster.")),

            new Topic("Pets",
                new Entry("How do I get a pet?",
                    "Hook a monster that is \"Twitching...\" and calm, then press Tame. Rarer monsters are harder to tame. You need room: build habitats for more."),
                new Entry("What are Familiar, Crew and Mystic?",
                    "Familiar: fights with you (pick one with \"Take into battle\") and fetches dead drifts and floating loot near the raft.\nCrew: repairs the raft, harpoon and modules, and can build parts.\nMystic: helps craft and makes hammers.\nSet their chores in Pets → Chores."),
                new Entry("How do pets grow?",
                    "Pets → pick a pet → Feed. Monster loot adds growth (its favourite element counts double). When the growth bar is full it evolves: Young → Adult → Elder, and gets stronger. Potions add stats, a few per stage."),
                new Entry("Do pets get old or get hurt?",
                    "Pets don't age. They have health instead: hits in battle, a monster lashing out, or a last thrash after a fight can hurt them or give them an ailment. A pet at 0 HP is knocked out: it rests and can't fight until it heals. Pets heal slowly on the raft, faster with each Tide Pool or Sky Perch. A Kelp Biscuit heals one right away (Pets → pick the pet)."),
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
