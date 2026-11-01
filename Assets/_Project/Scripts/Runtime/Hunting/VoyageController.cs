using System.Collections;
using System.Linq;
using BrineBattle.Rafts;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Leaving the sea (Ed's ask). With the Lv 6 raft, all five vehicle parts on and the Lv 6 boss
    /// beaten, the Raft tree's Leave button starts it: the plane takes off and climbs away, the sea
    /// train and the pirate ship speed off downstream. Then an ending screen, and a new, harder sea back at
    /// Lv 1 that keeps pets, gear and the bag. Finishing as many raft types as the difficulty asks wins
    /// the game. Added at runtime by HookController.
    /// </summary>
    public class VoyageController : MonoBehaviour
    {
        public HookController hook;
        public Raft raft;

        public static VoyageController Instance { get; private set; }
        bool leaving, ending;
        RaftKind left;
        Vector3 home;
        float wonAt = -1f;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public bool Busy => leaving || ending;

        /// <summary>Starts the departure if the raft is ready.</summary>
        public bool Leave()
        {
            if (Busy || raft == null || !raft.Grid.CanLeave || BattleController.InBattle) return false;
            left = raft.Grid.Chosen;
            WorldPause.Close(WorldPause.Open);
            WorldPause.Toggle(Menu.Voyage);
            StartCoroutine(Depart());
            return true;
        }

        IEnumerator Depart()
        {
            leaving = true;
            home = raft.transform.position;
            hook.Log($"Your {VesselRules.Vehicle(left)} is complete. {VesselRules.LeaveVerb(left)}!");
            const float time = 5f;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                float k = t / time, ease = k * k;
                // Downstream is +X. The plane climbs as it goes; the train and the ship just speed off.
                raft.transform.position = new Vector3(home.x + ease * 60f, raft.transform.position.y, home.z);
                raft.Lift = left == RaftKind.Aerial ? Mathf.Max(0f, k - 0.3f) * 18f : 0f;
                yield return null;
            }
            leaving = false;
            bool wasWon = hook.Voyage.Won;
            hook.Voyage.Leave(left);
            if (!wasWon && hook.Voyage.Won) wonAt = Time.unscaledTime;
            ending = true;
        }

        /// <summary>The ending screen's Continue: back home on a new sea at Lv 1.</summary>
        void NextSea()
        {
            ending = false;
            wonAt = -1f;
            raft.Lift = 0f;
            raft.transform.position = home;
            raft.Grid.NewSea(raft.starterPart, hook.Inventory);
            hook.SyncHabitats();
            WorldPause.Close(Menu.Voyage);
            hook.Log($"Sea {hook.Voyage.Sea} begins. Monsters here are tougher (×{hook.Voyage.EnemyScale:0.##}). The raft starts again at Lv 1."
                + (hook.Voyage.Won ? "" : $" Pick a raft type you haven't finished yet."));
            SaveSystem.Save(auto: true);
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!ending || hook == null) return;
            GUI.depth = -20;
            var v = hook.Voyage;
            float w = Mathf.Min(520, HuntStyles.W - 40), h = 330;
            GUILayout.BeginArea(new Rect((HuntStyles.W - w) / 2, (HuntStyles.H - h) / 2, w, h), "", HuntStyles.Window);
            GUILayout.Label($"<size=24><b>You left Sea {v.Sea - 1}!</b></size>", HuntStyles.Heading);
            GUILayout.Label($"Your {VesselRules.Vehicle(left)} carried you out of the sea.", HuntStyles.Text);
            GUILayout.Space(6);
            int done = v.Escaped.Distinct().Count();
            GUILayout.Label($"Raft types finished: <b>{done} / {v.TypesToWin}</b> ({v.Difficulty})  ·  "
                + string.Join(", ", v.Escaped.Distinct().Select(k => VesselRules.Vehicle(k))), HuntStyles.Text);
            if (wonAt >= 0f)
                GUILayout.Label($"<size=20><color={HuntStyles.GoodHex}><b>You beat BrineBattle Hunter on {v.Difficulty}!</b></color></size>\n"
                    + "Every raft type is open again. Keep sailing as long as you like.", HuntStyles.Heading);
            else if (v.Won)
                GUILayout.Label("The game is already won. Every raft type is open.", HuntStyles.Text);
            else
                GUILayout.Label($"Finish {v.TypesToWin - done} more raft type{(v.TypesToWin - done == 1 ? "" : "s")} to win.", HuntStyles.Text);
            GUILayout.Space(6);
            GUILayout.Label($"Next: Sea {v.Sea}. The raft starts again at Lv 1; your minions, gear and bag come along. "
                + $"Monsters there are ×{v.EnemyScale:0.##} as strong.", HuntStyles.Text);
            GUILayout.FlexibleSpace();
            if (HuntStyles.ColorButton($"Sail on to Sea {v.Sea}", HuntStyles.Go)) NextSea();
            GUILayout.EndArea();
        }
    }
}
