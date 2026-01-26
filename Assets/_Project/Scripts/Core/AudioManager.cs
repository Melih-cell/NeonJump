using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Header("Audio Sources")]
    private AudioSource musicSource;
    private AudioSource sfxSource;

    // Procedural ses kayitlari
    private AudioClip jumpSound;
    private AudioClip coinSound;
    private AudioClip hurtSound;
    private AudioClip enemyDeathSound;
    private AudioClip gameOverSound;
    private AudioClip winSound;
    private AudioClip buttonSound;
    private AudioClip powerUpSound;
    private AudioClip bossHitSound;
    private AudioClip fireSound;
    private AudioClip rollSound;

    // Muzik
    private AudioClip menuMusic;
    private AudioClip gameMusic;
    private AudioClip bossMusic;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeAudio()
    {
        // Audio source'lar olustur
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // Procedural sesleri olustur
        CreateSoundEffects();
        CreateMusic();
    }

    void CreateSoundEffects()
    {
        // Ziplama sesi - kisa yukari sweep
        jumpSound = CreateSound(0.1f, (t, duration) =>
        {
            float freq = Mathf.Lerp(200f, 600f, t / duration);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - t / duration) * 0.3f;
        });

        // Coin sesi - iki tonlu bling
        coinSound = CreateSound(0.15f, (t, duration) =>
        {
            float freq1 = 880f;
            float freq2 = 1320f;
            float phase = t < duration * 0.5f ? freq1 : freq2;
            float env = t < duration * 0.5f ? 1f : (1f - (t - duration * 0.5f) / (duration * 0.5f));
            return Mathf.Sin(2f * Mathf.PI * phase * t) * env * 0.25f;
        });

        // Hasar sesi - gürültülü düşüş
        hurtSound = CreateSound(0.3f, (t, duration) =>
        {
            float freq = Mathf.Lerp(400f, 100f, t / duration);
            float noise = (Random.value - 0.5f) * 0.3f;
            float env = 1f - t / duration;
            return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + noise) * env * 0.4f;
        });

        // Düşman ölüm sesi - patlama benzeri
        enemyDeathSound = CreateSound(0.2f, (t, duration) =>
        {
            float freq = Mathf.Lerp(300f, 50f, t / duration);
            float env = 1f - t / duration;
            float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
            return square * env * env * 0.3f;
        });

        // Game over sesi - üzgün melodi
        gameOverSound = CreateSound(0.8f, (t, duration) =>
        {
            float[] notes = { 440f, 392f, 349f, 330f };
            int noteIndex = Mathf.Min((int)(t / (duration / 4)), 3);
            float noteTime = t - noteIndex * (duration / 4);
            float env = Mathf.Exp(-noteTime * 5f);
            return Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * env * 0.3f;
        });

        // Kazanma sesi - mutlu arpej
        winSound = CreateSound(0.6f, (t, duration) =>
        {
            float[] notes = { 523f, 659f, 784f, 1047f };
            int noteIndex = Mathf.Min((int)(t / (duration / 4)), 3);
            float noteTime = t - noteIndex * (duration / 4);
            float env = Mathf.Exp(-noteTime * 3f);
            return Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * env * 0.3f;
        });

        // Buton sesi - kisa tik
        buttonSound = CreateSound(0.05f, (t, duration) =>
        {
            float freq = 800f;
            float env = 1f - t / duration;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.2f;
        });

        // Power-up sesi - büyülü ses
        powerUpSound = CreateSound(0.4f, (t, duration) =>
        {
            float freq = Mathf.Lerp(400f, 1200f, t / duration);
            float vibrato = Mathf.Sin(t * 30f) * 50f;
            float env = Mathf.Sin(t / duration * Mathf.PI);
            return Mathf.Sin(2f * Mathf.PI * (freq + vibrato) * t) * env * 0.3f;
        });

        // Boss hasar sesi - ağır vuruş
        bossHitSound = CreateSound(0.25f, (t, duration) =>
        {
            float freq = Mathf.Lerp(150f, 80f, t / duration);
            float env = 1f - t / duration;
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = (Random.value - 0.5f) * 0.2f;
            return (wave + noise) * env * 0.5f;
        });

        // Ates etme sesi - keskin patlama
        fireSound = CreateSound(0.15f, (t, duration) =>
        {
            float env = Mathf.Exp(-t * 30f); // Hizli decay
            float noise = (Random.value - 0.5f) * 2f;
            float freq = Mathf.Lerp(800f, 200f, t / duration);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            return (noise * 0.7f + tone * 0.3f) * env * 0.5f;
        });

        // Takla/Roll sesi - hizli swoosh
        rollSound = CreateSound(0.25f, (t, duration) =>
        {
            float env = Mathf.Sin(t / duration * Mathf.PI); // Fade in-out
            float noise = (Random.value - 0.5f);
            float freq = Mathf.Lerp(100f, 400f, t / duration);
            float whoosh = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f;
            return (noise * 0.5f + whoosh) * env * 0.4f;
        });
    }

    void CreateMusic()
    {
        // Menu müziği - sakin ambient
        menuMusic = CreateMusic(8f, (t, duration) =>
        {
            float bass = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.15f;
            float pad = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.1f;
            pad += Mathf.Sin(2f * Mathf.PI * 277f * t) * 0.08f;
            pad += Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.06f;

            // Yavaş LFO
            float lfo = (Mathf.Sin(t * 0.5f) + 1f) * 0.5f;
            return (bass + pad * lfo) * 0.5f;
        });

        // Oyun müziği - enerjik
        gameMusic = CreateMusic(4f, (t, duration) =>
        {
            // Tempo: 120 BPM -> 2 beat/saniye
            float beat = t * 2f;
            float beatPhase = beat % 1f;

            // Bass drum benzeri
            float kick = 0f;
            if (beatPhase < 0.1f)
            {
                float kickEnv = 1f - beatPhase / 0.1f;
                float kickFreq = Mathf.Lerp(150f, 50f, beatPhase / 0.1f);
                kick = Mathf.Sin(2f * Mathf.PI * kickFreq * t) * kickEnv * 0.3f;
            }

            // Hi-hat benzeri (off-beat)
            float hihat = 0f;
            float offBeat = (beat + 0.5f) % 1f;
            if (offBeat < 0.05f)
            {
                hihat = (Random.value - 0.5f) * (1f - offBeat / 0.05f) * 0.15f;
            }

            // Bas çizgisi
            float[] bassNotes = { 55f, 55f, 73f, 82f };
            int bassIndex = (int)(beat) % 4;
            float bass = Mathf.Sin(2f * Mathf.PI * bassNotes[bassIndex] * t) * 0.2f;

            // Arpej
            float[] arpNotes = { 220f, 277f, 330f, 440f, 330f, 277f };
            int arpIndex = (int)(beat * 2f) % 6;
            float arpEnv = 1f - (beat * 2f % 1f);
            float arp = Mathf.Sin(2f * Mathf.PI * arpNotes[arpIndex] * t) * arpEnv * 0.1f;

            return (kick + hihat + bass + arp) * 0.7f;
        });

        // Boss müziği - gergin
        bossMusic = CreateMusic(4f, (t, duration) =>
        {
            float beat = t * 2.5f; // Daha hızlı tempo
            float beatPhase = beat % 1f;

            // Ağır kick
            float kick = 0f;
            if (beatPhase < 0.15f)
            {
                float kickEnv = 1f - beatPhase / 0.15f;
                float kickFreq = Mathf.Lerp(100f, 30f, beatPhase / 0.15f);
                kick = Mathf.Sin(2f * Mathf.PI * kickFreq * t) * kickEnv * 0.4f;
            }

            // Gergin bas
            float bassFreq = 41f + Mathf.Sin(t * 2f) * 5f;
            float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.25f;

            // Distorted lead
            float lead = Mathf.Sin(2f * Mathf.PI * 165f * t);
            lead = Mathf.Sign(lead) * Mathf.Pow(Mathf.Abs(lead), 0.5f) * 0.15f;

            // Tremolo efekti
            float tremolo = (Mathf.Sin(t * 8f) + 1f) * 0.5f;

            return (kick + bass + lead * tremolo) * 0.7f;
        });
    }

    AudioClip CreateSound(float duration, System.Func<float, float, float> generator)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Clamp(generator(t, duration), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip CreateMusic(float duration, System.Func<float, float, float> generator)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Seed random for consistent music
        Random.InitState(42);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Clamp(generator(t, duration), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralMusic", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // === SES EFEKTLERI ===

    public void PlayJump()
    {
        PlaySFX(jumpSound);
    }

    public void PlayCoin()
    {
        PlaySFX(coinSound);
    }

    public void PlayHurt()
    {
        PlaySFX(hurtSound);
    }

    public void PlayEnemyDeath()
    {
        PlaySFX(enemyDeathSound);
    }

    public void PlayGameOver()
    {
        PlaySFX(gameOverSound);
    }

    public void PlayWin()
    {
        PlaySFX(winSound);
    }

    public void PlayButton()
    {
        PlaySFX(buttonSound);
    }

    public void PlayPowerUp()
    {
        PlaySFX(powerUpSound);
    }

    public void PlayBossHit()
    {
        PlaySFX(bossHitSound);
    }

    public void PlayFire()
    {
        PlaySFX(fireSound);
    }

    public void PlayRoll()
    {
        PlaySFX(rollSound);
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    // === MUZIK ===

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    public void PlayBossMusic()
    {
        PlayMusic(bossMusic);
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource != null)
        {
            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }

    // === VOLUME KONTROL ===

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    void UpdateVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    // === FADE ===

    public void FadeOutMusic(float duration)
    {
        StartCoroutine(FadeMusic(musicSource.volume, 0f, duration));
    }

    public void FadeInMusic(float duration)
    {
        StartCoroutine(FadeMusic(0f, musicVolume * masterVolume, duration));
    }

    System.Collections.IEnumerator FadeMusic(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            }
            yield return null;
        }

        if (to == 0f && musicSource != null)
        {
            musicSource.Stop();
        }
    }
}
