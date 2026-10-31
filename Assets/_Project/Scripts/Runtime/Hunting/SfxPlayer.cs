using BrineBattle.Audio;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>The sound-effect volume (Settings → Sound), 0-1. Default 0.7.</summary>
    public static class SfxSettings
    {
        public static float Volume
        {
            get => PlayerPrefs.GetFloat("brinebattle.sfxVolume", SfxRules.DefaultVolume);
            set => PlayerPrefs.SetFloat("brinebattle.sfxVolume", Mathf.Clamp01(value));
        }
    }

    /// <summary>
    /// Plays the code-made sound effects (<see cref="SfxSynth"/>) on a small pool of voices. World sounds pause
    /// while the Shipyard halts the game and pick up again on close; UI sounds keep playing there.
    /// Opening, switching and closing menus click by themselves. Added at runtime by the HookController.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        /// <summary>Play a sound if the player exists and it's allowed now. Safe to call from anywhere.</summary>
        public static void Play(Sfx s) { if (Instance != null) Instance.PlayOne(s); }

        /// <summary>Plays and returns true, for use inside a button condition: <c>if (Button(..) && DoIt() && Played(..))</c>.</summary>
        public static bool Played(Sfx s) { Play(s); return true; }

        AudioClip[] clips;
        AudioSource[] voices;
        Sfx[] playing;
        float[] endsAt, lastPlayed;
        bool halted;
        Menu lastMenu;
        readonly System.Random rng = new System.Random();

        void Awake()
        {
            Instance = this;
            int n = SfxSynth.All.Length;
            clips = new AudioClip[n];
            lastPlayed = new float[n];
            for (int i = 0; i < n; i++) lastPlayed[i] = -1f;
            voices = new AudioSource[SfxRules.Voices];
            playing = new Sfx[SfxRules.Voices];
            endsAt = new float[SfxRules.Voices];
            for (int i = 0; i < voices.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                voices[i] = s;
            }
            lastMenu = WorldPause.Open;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Made on first use (each is a few thousand samples), so start-up never stalls.</summary>
        AudioClip Clip(Sfx s)
        {
            int i = (int)s;
            if (clips[i] == null)
            {
                var data = SfxSynth.Make(s);
                clips[i] = AudioClip.Create($"Sfx {s}", data.Length, 1, SfxSynth.Rate, false);
                clips[i].SetData(data, 0);
            }
            return clips[i];
        }

        void PlayOne(Sfx s)
        {
            float vol = SfxSettings.Volume;
            if (vol <= 0f || !SfxRules.Plays(s, Shipyard.IsOpen)) return;
            float now = Time.unscaledTime;
            if (SfxRules.TooSoon(lastPlayed[(int)s], now)) return;
            lastPlayed[(int)s] = now;
            int v = SfxRules.PickVoice(endsAt, now);
            var src = voices[v];
            src.Stop();
            src.clip = Clip(s);
            src.pitch = SfxRules.Pitch(s, rng.NextDouble());
            src.volume = vol;
            src.Play();
            playing[v] = s;
            endsAt[v] = now + src.clip.length / src.pitch;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            // The Shipyard halts the world: world sounds wait (their voices stay busy) and resume on close.
            bool h = Shipyard.IsOpen;
            if (h != halted)
            {
                halted = h;
                for (int i = 0; i < voices.Length; i++)
                {
                    if (SfxRules.IsUi(playing[i]) || voices[i].clip == null) continue;
                    if (h && voices[i].isPlaying) { voices[i].Pause(); endsAt[i] = float.MaxValue; }
                    else if (!h && endsAt[i] == float.MaxValue)
                    {
                        voices[i].UnPause();
                        endsAt[i] = now + (voices[i].clip.length - voices[i].time) / voices[i].pitch;
                    }
                }
            }
            // Menus click by themselves: open, switch tab, close.
            var menu = WorldPause.Open;
            if (menu != lastMenu)
            {
                Play(lastMenu == Menu.None ? Sfx.MenuOpen : menu == Menu.None ? Sfx.MenuClose : Sfx.Tab);
                lastMenu = menu;
            }
        }
    }
}
