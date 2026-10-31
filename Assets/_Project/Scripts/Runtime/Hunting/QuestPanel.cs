using System.Collections.Generic;
using System.Linq;
using BrineBattle.Rafts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Quest tab (Ed's ask): a slim tab on the right edge of the screen (or J) opens the quest list on the left
    /// and the picked quest's checklist on the right, with a button to the place that fixes it. Quests come
    /// from <see cref="QuestRules"/>, rebuilt every time the panel draws. Added at runtime by HookController.
    /// </summary>
    public class QuestPanel : MonoBehaviour
    {
        public HookController hook;
        static bool open => WorldPause.Open == Menu.Quests;
        string picked;
        Vector2 listScroll, detailScroll;
        GUIStyle tabStyle;

        public static void Toggle() => WorldPause.Toggle(Menu.Quests);

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.jKey.wasPressedThisFrame && !BattleController.InBattle
                && (WorldPause.Open == Menu.None || WorldPause.Open == Menu.Quests)) Toggle();
        }

        List<Quest> Quests() => hook != null && hook.raft != null
            ? QuestRules.Build(hook.raft.Grid, hook.Inventory, hook.builder != null ? hook.builder.recipes.Where(r => r != null && hook.Knows(r)) : null)
            : new List<Quest>();

        const string JournalId = "journal", FameId = "fame";

        void OnGUI()
        {
            HuntStyles.Begin();
            if (hook == null || BattleController.InBattle) return;
            var quests = Quests();
            DrawTab(quests);
            if (!open) return;
            GUI.depth = -5;
            float w = Mathf.Min(600, HuntStyles.W - HookController.HudRight - 60), h = Mathf.Min(460, HuntStyles.H - 160);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 44, HuntStyles.H / 2f - h / 2f, w, h), $"Quests ({quests.Count(q => !q.Done)} open)", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Quests); GUILayout.EndArea(); return; }
            bool journal = picked == JournalId || picked == FameId || quests.Count == 0;
            var current = journal ? null : quests.FirstOrDefault(q => q.Id == picked) ?? quests.FirstOrDefault(q => !q.Done) ?? quests[0];
            if (!journal) picked = current.Id;

            GUILayout.BeginHorizontal();
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Width(200));
            foreach (var q in quests)
            {
                var old = GUI.backgroundColor;
                if (q == current) GUI.backgroundColor = HuntStyles.Go;
                string mark = q.Done ? "<color=#7be08a>✔</color>" : $"<color=#ffc840>{q.Missing}</color>";
                if (GUILayout.Button($"{mark} <b>{q.Title}</b>\n<size=11>{q.Summary}</size>", HuntStyles.RichButton, GUILayout.Height(44))) picked = q.Id;
                GUI.backgroundColor = old;
            }
            // The sea journal of firsts (C-15), after the quests.
            var bg = GUI.backgroundColor;
            if (journal) GUI.backgroundColor = HuntStyles.Go;
            if (GUILayout.Button($"<color=#ffd866>★</color> <b>Sea journal</b>\n<size=11>{hook.Firsts.Count}/{World.Firsts.All.Length} firsts</size>", HuntStyles.RichButton, GUILayout.Height(44))) picked = JournalId;
            GUI.backgroundColor = bg;
            // Fame and achievements (Ed's ask: the goal is fame), after the journal.
            if (picked == FameId) GUI.backgroundColor = HuntStyles.Go;
            if (GUILayout.Button($"<color=#ffd866>🏆</color> <b>Fame {hook.Fame.Points}</b>\n<size=11>{hook.Fame.Rank} · {hook.Fame.Unlocked.Count()}/{Rafts.FameBook.All.Length}</size>", HuntStyles.RichButton, GUILayout.Height(44))) picked = FameId;
            GUI.backgroundColor = bg;
            GUILayout.EndScrollView();

            if (picked == FameId)
            {
                detailScroll = GUILayout.BeginScrollView(detailScroll);
                GUILayout.Label($"Fame {hook.Fame.Points} · {hook.Fame.Rank}", HuntStyles.Heading);
                GUILayout.Label($"Monsters {hook.Fame.Kills} · bosses {hook.Fame.Bosses} · tames {hook.Fame.Tames} · crafts {hook.Fame.Crafts} · best raft Lv {hook.Fame.BestRaft} · best ship Lv {hook.Fame.BestShip} · {hook.Voyage.Difficulty} ×{Rafts.FameBook.Multiplier(hook.Voyage.Difficulty):0.#}", HuntStyles.Hint);
                foreach (var a in Rafts.FameBook.All)
                {
                    bool got = hook.Fame.Has(a.Id);
                    GUILayout.BeginHorizontal(HuntStyles.Card);
                    GUILayout.Label(got ? "<color=#7be08a>✔</color>" : "<color=#9bb0c8>○</color>", HuntStyles.Text, GUILayout.Width(18));
                    GUILayout.Label($"<b>{a.Title}</b>\n<size=11>{a.Goal}  <color=#ffd866>+{a.Fame} fame</color></size>", HuntStyles.Text);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }

            if (journal)
            {
                detailScroll = GUILayout.BeginScrollView(detailScroll);
                GUILayout.Label("Sea journal", HuntStyles.Heading);
                GUILayout.Label("Small firsts on your voyage. Each one gives a little essence and Alchemy Mastery, once.", HuntStyles.Hint);
                foreach (var e in World.Firsts.All)
                {
                    bool got = hook.Firsts.Has(e.Id);
                    GUILayout.BeginHorizontal(HuntStyles.Card);
                    GUILayout.Label(got ? "<color=#7be08a>✔</color>" : "<color=#9bb0c8>○</color>", HuntStyles.Text, GUILayout.Width(18));
                    GUILayout.Label($"<b>{e.Title}</b>\n<size=11>{e.How}  <color=#ffd866>+{e.Essence} essence, +{e.Mastery} Mastery</color></size>", HuntStyles.Text);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }

            detailScroll = GUILayout.BeginScrollView(detailScroll);
            GUILayout.Label(current.Title, HuntStyles.Heading);
            if (!string.IsNullOrEmpty(current.Summary)) GUILayout.Label(current.Summary, HuntStyles.Hint);
            GUILayout.Space(4);
            foreach (var l in current.Lines)
            {
                GUILayout.BeginHorizontal(HuntStyles.Card);
                GUILayout.Label(l.Done ? "<color=#7be08a>✔</color>" : "<color=#ff7a6a>✘</color>", HuntStyles.Text, GUILayout.Width(18));
                GUILayout.Label(l.Text, HuntStyles.Text);
                GUILayout.FlexibleSpace();
                if (l.Need > 1 || l.Have > 1) GUILayout.Label(HuntStyles.Count(l.Have, l.Need), HuntStyles.Text, GUILayout.Width(70));
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(6);
            string go = GoLabel(current);
            if (go != null && HuntStyles.ColorButton(go, current.Done || current.Target == QuestTarget.Boss ? HuntStyles.Go : HuntStyles.Other)) Go(current);
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>The slim tab tucked at the right edge: open quests count, ready ones in green.</summary>
        void DrawTab(List<Quest> quests)
        {
            tabStyle ??= new GUIStyle(GUI.skin.button) { richText = true, fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            int openCount = quests.Count(q => !q.Done), ready = quests.Count(q => q.Done && q.Target != QuestTarget.None);
            var r = new Rect(HuntStyles.W - 36, HuntStyles.H / 2f - 70, 32, 140);
            var old = GUI.backgroundColor;
            GUI.backgroundColor = open ? HuntStyles.Go : ready > 0 ? new Color(0.5f, 0.95f, 0.55f) : new Color(1f, 0.85f, 0.45f);
            if (GUI.Button(r, $"Q\nU\nE\nS\nT\n<size=11>{openCount}</size>", tabStyle)) Toggle();
            GUI.backgroundColor = old;
        }

        static string GoLabel(Quest q)
        {
            switch (q.Target)
            {
                case QuestTarget.RaftTree: return "Open the Raft tree";
                case QuestTarget.Build: return "Open Build";
                case QuestTarget.Workbench: return $"Open Workbench → {Items.CraftRules.Label(q.Sub)}";
                case QuestTarget.Leave: return q.Done ? "Leave the sea" : null;
                // Ed's ask: call the boss yourself once its monsters are beaten, instead of waiting for it.
                case QuestTarget.Boss: return q.Lines.Count > 0 && q.Lines[0].Done ? "Summon the boss now!" : null;
                default: return null;
            }
        }

        void Go(Quest q)
        {
            var b = hook.builder;
            WorldPause.Close(Menu.Quests);
            switch (q.Target)
            {
                case QuestTarget.RaftTree: b?.OpenTree(); break;
                case QuestTarget.Build: b?.Toggle(); break;
                case QuestTarget.Workbench: b?.OpenBench(q.Sub); break;
                case QuestTarget.Leave: VoyageController.Instance?.Leave(); break;
                case QuestTarget.Boss: hook.SummonBoss(); break;
            }
        }
    }
}
