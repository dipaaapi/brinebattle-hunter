using System.Linq;
using BrineBattle.Audio;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>The music volume (Settings → Game), 0-1. Default 0.6.</summary>
    public static class MusicSettings
    {
        public static float Volume
        {
            get => PlayerPrefs.GetFloat("brinebattle.musicVolume", 0.6f);
            set => PlayerPrefs.SetFloat("brinebattle.musicVolume", Mathf.Clamp01(value));
        }
    }

    /// <summary>
    /// Plays the layered BGM (Phase 9, Claude's C-20). The loops are made in code by <see cref="MusicSynth"/>
    /// at start, all start together, and each layer fades toward the volume <see cref="MusicRules"/> picks
    /// for the moment: day or night, weather, something alive near the raft, a battle or a boss.
    /// Placeholder music until real tracks replace the clips. Added at runtime by the HookController.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public HookController hook;

        AudioSource[] sources;
        float[] volumes;
        bool ready;
        float scan;
        bool danger;

        System.Collections.IEnumerator Start()
        {
            int n = MusicRules.Layers;
            sources = new AudioSource[n];
            volumes = new float[n];
            var clips = new AudioClip[n];
            // One layer a frame, so making the music never stalls the game.
            for (int i = 0; i < n; i++)
            {
                var layer = (MusicLayer)i;
                var data = MusicSynth.Make(layer);
                clips[i] = AudioClip.Create($"Music {layer}", data.Length, 1, MusicSynth.Rate, false);
                clips[i].SetData(data, 0);
                yield return null;
            }
            for (int i = 0; i < n; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.clip = clips[i];
                s.loop = true;
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.volume = 0f;
                sources[i] = s;
            }
            // Start them on the same DSP tick so the layers stay in time forever.
            double at = AudioSettings.dspTime + 0.2;
            foreach (var s in sources) s.PlayScheduled(at);
            ready = true;
        }

        void Update()
        {
            if (!ready) return;
            var m = Moment();
            var targets = MusicRules.Targets(m);
            float master = MusicSettings.Volume;
            for (int i = 0; i < sources.Length; i++)
            {
                float step = Time.unscaledDeltaTime / MusicRules.FadeSeconds((MusicLayer)i);
                volumes[i] = Mathf.MoveTowards(volumes[i], targets[i], step);
                sources[i].volume = volumes[i] * master;
            }
        }

        MusicMoment Moment()
        {
            // "Something moves!" near the raft, by the hint the player sees (never the hidden truth).
            if (Time.unscaledTime >= scan && hook != null && hook.raft != null)
            {
                scan = Time.unscaledTime + 0.5f;
                var center = hook.raft.transform.position;
                float near = hook.raft.EdgeDistance + 6f;
                danger = FindObjectsByType<Drift>(FindObjectsSortMode.None).Any(d => d != null && !d.Monster.isFlotsam
                    && (d.Hint == Encounters.DriftState.Alive || d.Monster.isBoss || d.GorgedParts > 0)
                    && Vector3.Distance(new Vector3(d.transform.position.x, 0f, d.transform.position.z), new Vector3(center.x, 0f, center.z)) <= near);
            }
            var now = WorldClock.Now;
            var foe = BattleController.Foe;
            return new MusicMoment
            {
                Daylight = now != null ? World.WorldRules.Daylight(now.Time01) : 1f,
                Storm = WorldClock.Weather == World.Weather.Storm || WorldClock.Weather == World.Weather.Rough,
                Rain = WorldClock.Weather == World.Weather.Rain,
                Danger = danger,
                InBattle = BattleController.InBattle,
                BossBattle = BattleController.InBattle && foe != null && foe.Monster.isBoss,
                MenuOpen = WorldPause.Open != Menu.None,
            };
        }
    }
}
