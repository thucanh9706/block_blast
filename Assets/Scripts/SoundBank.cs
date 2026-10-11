using UnityEngine;

/// <summary>Tự tổng hợp âm thanh bằng code (không cần file audio) và phát qua pool AudioSource.</summary>
public class SoundBank : MonoBehaviour
{
    public AudioClip Place, Pick, Invalid, GameOver, Click, NewBest, Combo;
    public AudioClip[] Clears = new AudioClip[5];
    public bool Muted;

    AudioSource[] pool;
    int next;

    struct Note
    {
        public float f, to, t, len, vol;
        public Note(float f, float t, float len, float vol, float to = -1f)
        {
            this.f = f; this.t = t; this.len = len; this.vol = vol; this.to = to < 0f ? f : to;
        }
    }

    void Awake()
    {
        pool = new AudioSource[6];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }

        Place = Synth("place", 0.2f, new Note(170f, 0f, 0.14f, 0.9f, 80f), new Note(420f, 0f, 0.05f, 0.25f));
        Pick = Synth("pick", 0.1f, new Note(660f, 0f, 0.07f, 0.5f, 800f));
        Invalid = Synth("invalid", 0.2f, new Note(170f, 0f, 0.16f, 0.6f, 110f));
        Click = Synth("click", 0.08f, new Note(900f, 0f, 0.05f, 0.45f));
        GameOver = Synth("gameover", 1.2f,
            new Note(392f, 0f, 0.4f, 0.6f), new Note(330f, 0.22f, 0.4f, 0.6f),
            new Note(262f, 0.44f, 0.4f, 0.6f), new Note(196f, 0.66f, 0.5f, 0.7f));
        NewBest = Synth("newbest", 1.0f,
            new Note(523.25f, 0.05f, 0.3f, 0.55f), new Note(659.25f, 0.18f, 0.3f, 0.55f),
            new Note(783.99f, 0.31f, 0.3f, 0.55f), new Note(1046.5f, 0.44f, 0.5f, 0.6f));
        Combo = Synth("combo", 0.5f, new Note(1046.5f, 0f, 0.2f, 0.45f), new Note(1318.5f, 0.07f, 0.25f, 0.45f));

        float[] semis = { 0, 4, 7, 12, 16, 19, 24 };
        for (int lines = 1; lines <= 5; lines++)
        {
            int count = Mathf.Min(2 + lines, semis.Length);
            var notes = new Note[count];
            for (int i = 0; i < count; i++)
                notes[i] = new Note(523.25f * Mathf.Pow(2f, semis[i] / 12f), 0.055f * i, 0.28f, 0.55f);
            Clears[lines - 1] = Synth("clear" + lines, 0.055f * count + 0.35f, notes);
        }
    }

    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (Muted || clip == null || pool == null) return;
        var src = pool[next];
        next = (next + 1) % pool.Length;
        src.pitch = pitch;
        src.PlayOneShot(clip, volume);
    }

    public void PlayClear(int lines, int streak)
    {
        float pitch = 1f + 0.05f * (Mathf.Clamp(streak, 1, 9) - 1);
        Play(Clears[Mathf.Clamp(lines, 1, 5) - 1], 1f, pitch);
        if (streak >= 2) Play(Combo, 0.8f, 1f + 0.04f * Mathf.Min(streak, 8));
    }

    static AudioClip Synth(string name, float total, params Note[] notes)
    {
        const int rate = 44100;
        int n = Mathf.CeilToInt(total * rate);
        var data = new float[n];

        foreach (var nt in notes)
        {
            int s0 = Mathf.FloorToInt(nt.t * rate);
            int len = Mathf.FloorToInt(nt.len * rate);
            double phase = 0.0;
            for (int i = 0; i < len && s0 + i < n; i++)
            {
                float p = (float)i / len;
                float t = (float)i / rate;
                float f = Mathf.Lerp(nt.f, nt.to, p);
                phase += 2.0 * System.Math.PI * f / rate;
                float env = Mathf.Min(1f, t / 0.004f) * (1f - p) * (1f - p);
                float s = Mathf.Sin((float)phase) + 0.35f * Mathf.Sin((float)(2.0 * phase)) + 0.12f * Mathf.Sin((float)(3.0 * phase));
                data[s0 + i] += s * env * nt.vol * 0.45f;
            }
        }
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);

        var clip = AudioClip.Create(name, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
