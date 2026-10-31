#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Encounters;
using BrineBattle.Items;
using BrineBattle.Monsters;
using BrineBattle.Rafts;
using BrineBattle.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Developer panel (Ed's ask): F1 opens it in the Editor and in Development Builds only. Jump the raft's
    /// level and see why it can't go up ("Why not?"), fill the bag, tame and level minions, summon or end battles,
    /// god mode, speed up time and change the weather, and save/load named snapshots of a scenario. Every action
    /// is logged and marks the game DEV. Added at runtime by HookController.
    /// </summary>
    public class DevPanel : MonoBehaviour
    {
        public HookController hook;
        bool open;
        int tab;
        Vector2 scroll;
        string filter = "", snapshot = "scenario";
        int speed = 1, devLevel;
        static readonly string[] Tabs = { "Raft", "Bag", "Minions", "Battle", "World", "Saves", "Log" };
        static readonly int[] Speeds = { 1, 3, 10 };

        static string SnapshotFolder => Path.Combine(SaveSystem.Folder, "Dev");

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) open = !open;
            var battle = BattleController.Current;
            if (DevMode.God && battle != null && battle.Outcome == BattleOutcome.Ongoing) battle.DevHeal();
            // Time speed outside battles; the boss show's slow motion and battles run at their own pace.
            if (!BattleController.InBattle && speed != 1) Time.timeScale = speed;
            else if (speed != 1 && Time.timeScale == speed) Time.timeScale = 1f;
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (hook == null) return;
            GUI.depth = -20;
            if (DevMode.Used) GUI.Label(new Rect(HuntStyles.W / 2f - 60, 4, 120, 20), "<color=#ff6b6b><b>DEV</b></color>  <size=10>F1</size>", new GUIStyle(HuntStyles.Text) { alignment = TextAnchor.MiddleCenter });
            if (!open) return;
            float w = Mathf.Min(560, HuntStyles.W - 40), h = Mathf.Min(560, HuntStyles.H - 60);
            GUILayout.BeginArea(new Rect(20, 30, w, h), "Developer (F1)", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { open = false; GUILayout.EndArea(); return; }
            tab = HuntStyles.Tabs(tab, Tabs);
            scroll = GUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case 0: RaftTab(); break;
                case 1: BagTab(); break;
                case 2: MinionsTab(); break;
                case 3: BattleTab(); break;
                case 4: WorldTab(); break;
                case 5: SavesTab(); break;
                default: foreach (var l in DevMode.Log.Reverse()) GUILayout.Label(l, HuntStyles.Hint); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void Did(string what) => DevMode.Did(what, hook);

        bool Button(string label, float width = 0f) =>
            width > 0f ? GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(26)) : GUILayout.Button(label, GUILayout.Height(26));

        // ---- Raft ----

        void RaftTab()
        {
            var g = hook.raft.Grid;
            var bag = hook.Inventory;
            HuntStyles.Section($"{g.LevelName} ({g.FormName}) · {g.Tier} · kills {g.Kills}/{g.KillsNeeded}");
            // Level slider: raft Lv 1-50, ship Lv 1-100 (Ed's ask).
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Lv {devLevel}", GUILayout.Width(56));
            devLevel = Mathf.RoundToInt(GUILayout.HorizontalSlider(devLevel <= 0 ? g.Level : devLevel, 1, g.MaxLevel));
            if (Button("Set", 60)) { g.DevSetLevel(g.Chosen, devLevel); Did($"{g.LevelName}"); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Lv −1") && g.Level > 1) { g.DevSetLevel(g.Chosen, g.Level - 1); devLevel = g.Level; Did(g.LevelName); }
            if (Button("Lv +1 (free)") && !g.AtMax) { g.DevSetLevel(g.Chosen, g.Level + 1); devLevel = g.Level; Did(g.LevelName); }
            if (Button("Max level")) { g.DevSetLevel(g.Chosen, g.MaxLevel); devLevel = g.Level; Did(g.LevelName); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Back to a raft")) { g.DevSetLevel(RaftKind.Plain, EvolutionRules.RaftMax); devLevel = g.Level; Did("back to a Lv 50 raft"); }
            foreach (var k in RaftTree.Kinds)
                if (Button($"{k} ship")) { g.DevSetLevel(k, 1); devLevel = 1; hook.raft.Grid.ResetOffType(null, hook.Inventory); Did($"made it a {EvolutionRules.LineName(k)} Lv 1"); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Fill what the next level needs")) FillNextLevel();
            if (Button("Level up (pay)") && g.TryUpgrade(bag, g.Chosen)) Did($"leveled up (paid): {g.LevelName}");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Kills = needed")) { g.Kills = g.KillsNeeded; Did("kills set to what the level needs"); }
            if (Button("Mark this tier's boss beaten")) { g.MarkBoss(g.Tier); Did($"{g.Tier} boss marked beaten"); }
            GUILayout.EndHorizontal();

            // "Why not?": the blockers the game itself checks, straight from the rules.
            HuntStyles.Section("Why not?");
            Why("Level up", g.UpgradeBlocker(bag));
            Why("Pick the ship line", g.ChooseBlocker());
            Why("Boss due", BossRules.Due(g) ? null : !g.BossesNeeded ? "bosses are off" : !EvolutionRules.BossGate(g.Chosen, g.Level) ? "no boss at this level (bands end at raft 16/33/50, ship 33/66/99)" : g.BossBeaten(g.Tier) ? "already beaten" : $"needs {g.KillsNeeded - g.Kills} more kills");
            Why("Ship part", g.PartPending == 0 ? null : $"put on the {VesselRules.PartName(g.Chosen, g.PartPending)}");
            foreach (var q in QuestRules.Build(g, bag, hook.builder != null ? hook.builder.recipes.Where(hook.Knows) : null).Where(q => !q.Done))
                Why(q.Title, string.Join(", ", q.Lines.Where(l => !l.Done).Select(l => $"{l.Text} {l.Have}/{l.Need}")));
        }

        static void Why(string what, string blocker) =>
            GUILayout.Label(blocker == null ? $"<color=#7be08a>✔</color> {what}: ready" : $"<color=#ff7a6a>✘</color> {what}: {blocker}", HuntStyles.Text);

        void FillNextLevel()
        {
            var g = hook.raft.Grid;
            var bag = hook.Inventory;
            g.Kills = Math.Max(g.Kills, g.KillsNeeded);
            var mat = Items().FirstOrDefault(i => i.type == ItemType.Material && i.raftKind == RaftKind.Plain);
            if (mat != null) bag.Add(mat, Math.Max(0, g.MaterialsNeeded - bag.MaterialCount()));
            bag.AddEssence(new EssenceKey(g.Tier, Elements.Element.None), g.EssenceNeeded);
            if (!RaftRules.HasHammer(bag, g.Tier))
            {
                var hammer = Items().Where(i => i.type == ItemType.Tool && i.rarity >= g.Tier).OrderBy(i => i.rarity).FirstOrDefault();
                if (hammer != null) bag.Add(hammer, 1);
            }
            g.MarkBoss(g.Tier);
            if (g.PartPending > 0)
            {
                var part = Items().FirstOrDefault(i => i.type == ItemType.Vessel && i.raftKind == g.Chosen && i.vesselSlot == g.PartPending);
                if (part != null) { bag.Add(part, 1); g.InstallVessel(part, bag); }
            }
            Did($"filled what {g.LevelName} needs to go up (modules on the left edge are still yours to place)");
        }

        // ---- Bag ----

        System.Collections.Generic.IEnumerable<ItemDefinition> Items()
        {
            var book = FindFirstObjectByType<CollectionBook>();
            var fromBook = book != null ? book.items : new System.Collections.Generic.List<ItemDefinition>();
            var fromRecipes = hook.builder != null ? hook.builder.recipes.Where(r => r != null).SelectMany(r => r.inputs.Concat(r.outputs)).Select(x => x.item) : Enumerable.Empty<ItemDefinition>();
            return fromBook.Concat(fromRecipes).Where(i => i != null).Distinct().OrderBy(i => i.type).ThenBy(i => i.rarity).ThenBy(i => i.displayName);
        }

        void BagTab()
        {
            var bag = hook.Inventory;
            HuntStyles.Section("Essence");
            GUILayout.BeginHorizontal();
            foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
                if (Button($"+20 {r}")) { bag.AddEssence(new EssenceKey(r, Elements.Element.None), 20); Did($"+20 {r} essence"); }
            GUILayout.EndHorizontal();
            HuntStyles.Section("Items");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Find", GUILayout.Width(40));
            filter = GUILayout.TextField(filter ?? "");
            GUILayout.EndHorizontal();
            foreach (var i in Items().Where(i => string.IsNullOrEmpty(filter) || i.displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).Take(60))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color={BagPanel.RarityColor(i.rarity)}>{i.displayName}</color> <size=10>{i.type} · have {bag.Count(i)}</size>", HuntStyles.Text);
                if (Button("+1", 44)) { bag.Add(i, 1); Did($"+1 {i.displayName}"); }
                if (Button("+10", 50)) { bag.Add(i, 10); Did($"+10 {i.displayName}"); }
                GUILayout.EndHorizontal();
            }
        }

        // ---- Minions ----

        System.Collections.Generic.IEnumerable<MonsterDefinition> Monsters()
        {
            var s = FindFirstObjectByType<DriftSpawner>();
            return s == null ? Enumerable.Empty<MonsterDefinition>() : s.monsters.Where(m => m != null && !m.isFlotsam).OrderBy(m => m.rarity).ThenBy(m => m.displayName);
        }

        void MinionsTab()
        {
            var roster = hook.Companions;
            HuntStyles.Section($"Your minions {roster.All.Count}/{roster.Capacity}");
            foreach (var c in roster.All.ToList())
            {
                GUILayout.BeginVertical(HuntStyles.Card);
                GUILayout.Label($"<b>{c.Name}</b> Lv {c.Level} · {c.Rarity} · {c.StageName} · {MinionRules.Skills(c)}", HuntStyles.Text);
                GUILayout.BeginHorizontal();
                if (Button("+1 Lv")) { Level(c, c.Level + 1); }
                if (Button("Lv 10")) Level(c, 10);
                if (Button("Lv 20")) Level(c, 20);
                if (Button("Lv 30")) Level(c, MinionRules.MaxLevel);
                if (Button("Heal")) { c.Hp = c.MaxHp; c.Status = StatusEffect.None; Did($"healed {c.Name}"); }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                foreach (var s in MinionRules.All.Where(s => !c.Has(s)).Take(6))
                    if (Button($"+{s}")) { c.Learn(s); roster.Refresh(); hook.ApplyCompanionBonuses(); Did($"{c.Name} learned {s}"); }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            HuntStyles.Section("Tame any monster (skips the roll and the room check)");
            foreach (var m in Monsters())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color={BagPanel.RarityColor(m.rarity)}>{m.displayName}</color> <size=10>{m.element} · {m.habitat}</size>", HuntStyles.Text);
                if (Button("Tame", 70))
                {
                    if (roster.Full) roster.Habitats++;
                    var c = Companion.FromMonster(m);
                    roster.Add(c);
                    hook.ApplyCompanionBonuses();
                    Did($"tamed {m.displayName}");
                }
                GUILayout.EndHorizontal();
            }
        }

        void Level(Companion c, int level)
        {
            level = Mathf.Clamp(level, 1, MinionRules.MaxLevel);
            int exp = 0;
            for (int l = c.Level; l < level; l++) exp += MinionRules.ExpToNext(l);
            c.GainExp(exp - c.Exp);
            hook.Companions.Refresh();
            hook.ApplyCompanionBonuses();
            Did($"{c.Name} set to Lv {c.Level}");
        }

        // ---- Battle ----

        void BattleTab()
        {
            var battle = BattleController.Current;
            DevMode.God = GUILayout.Toggle(DevMode.God, " God mode: you, the battle minion and the raft stay full in battle");
            HuntStyles.Section(battle != null ? $"In battle: {battle.Monster.Name} {battle.Monster.Hp}/{battle.Monster.MaxHp}" : "No battle");
            GUILayout.BeginHorizontal();
            GUI.enabled = battle != null && battle.Outcome == BattleOutcome.Ongoing;
            if (Button("Win now")) { battle.DevFinish(BattleOutcome.Won); Did("won the battle"); }
            if (Button("Lose (knocked out)")) { battle.DevFinish(BattleOutcome.LostHunter); Did("lost the battle"); }
            if (Button("Lose (raft breaks)")) { battle.DevFinish(BattleOutcome.LostRaft); Did("lost the battle (raft)"); }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            HuntStyles.Section("Bosses");
            GUI.enabled = battle == null;
            if (Button("Summon this level's boss now (due or not)")) { hook.SummonBoss(true); Did("summoned the boss"); }
            GUI.enabled = true;
            HuntStyles.Section("Spawn a monster next to the raft");
            var spawner = FindFirstObjectByType<DriftSpawner>();
            foreach (var m in Monsters().Concat(spawner != null ? spawner.DevBosses : Enumerable.Empty<MonsterDefinition>()))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color={BagPanel.RarityColor(m.rarity)}>{m.displayName}</color>{(m.isBoss ? " <size=10>BOSS</size>" : "")}", HuntStyles.Text);
                foreach (var st in new[] { DriftState.Alive, DriftState.Weakened, DriftState.Dead })
                    if (Button(st.ToString(), 80) && spawner != null) { spawner.DevSpawn(m, st); Did($"spawned {m.displayName} ({st})"); }
                GUILayout.EndHorizontal();
            }
        }

        // ---- World ----

        void WorldTab()
        {
            HuntStyles.Section("Time speed (outside battles)");
            GUILayout.BeginHorizontal();
            foreach (var s in Speeds)
            {
                var old = GUI.backgroundColor;
                if (speed == s) GUI.backgroundColor = HuntStyles.Gold;
                if (Button($"×{s}")) { speed = s; if (s == 1) Time.timeScale = 1f; Did($"time ×{s}"); }
                GUI.backgroundColor = old;
            }
            GUILayout.EndHorizontal();
            var now = WorldClock.Now;
            if (now != null)
            {
                HuntStyles.Section($"Day {now.Day} · {now.Time} · {now.Weather} (next {now.Next})");
                GUILayout.BeginHorizontal();
                foreach (var (label, t) in new[] { ("Dawn", 0.02f), ("Day", 0.3f), ("Dusk", WorldRules.DuskStarts + 0.01f), ("Night", WorldRules.NightStarts + 0.05f) })
                    if (Button(label)) { now.Time01 = t; Did($"time set to {label}"); }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                foreach (Weather w in Enum.GetValues(typeof(Weather)))
                    if (Button(w.ToString())) { now.Weather = w; now.WeatherLeft = WorldRules.MaxWeatherSeconds; Did($"weather set to {w}"); }
                GUILayout.EndHorizontal();
            }
            HuntStyles.Section($"Alchemy Mastery Lv {hook.Mastery.Level}");
            if (Button("Max Mastery (all recipes)")) { hook.Mastery.Add(AlchemyMastery.PointsFor(AlchemyMastery.MaxLevel)); Did("mastery maxed"); }
            HuntStyles.Section("Hunter");
            if (Button("Full HP, no ailment, harpoon fixed"))
            {
                hook.HunterBody.Hp = hook.HunterBody.MaxHp;
                hook.HunterBody.Status = StatusEffect.None;
                hook.Harpoon.Set(hook.Harpoon.Max);
                Did("hunter healed");
            }
        }

        // ---- Saves ----

        void SavesTab()
        {
            HuntStyles.Section("Snapshots (named saves of a scenario, kept in Saves/Dev)");
            GUILayout.BeginHorizontal();
            snapshot = GUILayout.TextField(snapshot ?? "");
            if (Button("Save snapshot", 130)) SaveSnapshot();
            GUILayout.EndHorizontal();
            GUILayout.Label("Loading a snapshot replaces save.json (the old one goes to save.backup.json).", HuntStyles.Hint);
            if (!Directory.Exists(SnapshotFolder)) return;
            foreach (var f in Directory.GetFiles(SnapshotFolder, "*.json").OrderByDescending(File.GetLastWriteTime))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{Path.GetFileNameWithoutExtension(f)} <size=10>{File.GetLastWriteTime(f):MM-dd HH:mm}</size>", HuntStyles.Text);
                if (Button("Load", 70)) LoadSnapshot(f);
                if (Button("Delete", 70)) { File.Delete(f); Did($"deleted snapshot {Path.GetFileName(f)}"); }
                GUILayout.EndHorizontal();
            }
        }

        void SaveSnapshot()
        {
            string name = string.Concat((snapshot ?? "").Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == ' ')).Trim();
            if (name.Length == 0) name = "scenario";
            DevMode.Did($"snapshot \"{name}\"", hook);
            string result = SaveSystem.Save();
            try
            {
                Directory.CreateDirectory(SnapshotFolder);
                File.Copy(SaveSystem.FilePath, Path.Combine(SnapshotFolder, name + ".json"), true);
                hook.Log($"[DEV] Snapshot \"{name}\" saved. {result}");
            }
            catch (Exception e) { hook.Log($"[DEV] Snapshot failed: {e.Message}"); }
        }

        void LoadSnapshot(string file)
        {
            if (BattleController.InBattle) { hook.Log("[DEV] Finish the battle first."); return; }
            try
            {
                Directory.CreateDirectory(SaveSystem.Folder);
                if (SaveSystem.Exists) File.Copy(SaveSystem.FilePath, SaveSystem.BackupPath, true);
                File.Copy(file, SaveSystem.FilePath, true);
            }
            catch (Exception e) { hook.Log($"[DEV] Snapshot load failed: {e.Message}"); return; }
            string result = SaveSystem.Load();
            DevMode.Did($"loaded snapshot {Path.GetFileNameWithoutExtension(file)}: {result}", hook);
        }
    }
}
#endif
