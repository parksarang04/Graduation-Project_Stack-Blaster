// ====================================================
// 오디오 매니저
// mp3 파일 없이 AudioClip.Create()로 사운드 직접 생성함
// (오실레이터 파형 - 사인/스퀘어/톱니/삼각파 계산해서 샘플 데이터 채움)
// BGM은 16비트 시퀀서 패턴으로 코루틴 돌리면서 재생
// 점수 10점마다 currentStage 올라가면서 악기 레이어 추가됨
// ====================================================
using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Settings")]
    public int sampleRate = 44100;
    [Range(0f, 1f)] public float masterVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource kickSource, hihatSource, snareSource, bassSource, arpSource;

    private bool muted = false;
    private bool paused = false;
    private int currentStage = 0;

    private Coroutine bgmRoutine;

    // C major 음계
    private static readonly float[] SCALE = {
        261.63f, 293.66f, 329.63f, 349.23f, 392f, 440f, 493.88f, 523.25f,
        587.33f, 659.25f, 698.46f, 783.99f, 880f
    };

    // A minor 베이스라인
    private static readonly float[] BASS_NOTES = { 55, 55, 65, 55, 55, 55, 65, 73, 55, 55, 65, 55, 73, 65, 55, 48 };
    private static readonly float[] ARP_PATTERN = { 440, 523.25f, 587.33f, 659.25f, 783.99f, 880, 659.25f, 523.25f };

    private static readonly int[] KICK_PATTERN = { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
    private static readonly int[] HIHAT_PATTERN = { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0 };
    private static readonly int[] SNARE_PATTERN = { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 };

    void Awake()
    {
        Instance = this;
        sfxSource = CreateSource("SFX");
        kickSource = CreateSource("Kick");
        hihatSource = CreateSource("Hihat");
        snareSource = CreateSource("Snare");
        bassSource = CreateSource("Bass");
        arpSource = CreateSource("Arp");
    }

    AudioSource CreateSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = transform;
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }

    //  외부 API 
    public void SetMuted(bool value) { muted = value; }
    public void SetPaused(bool value)
    {
        paused = value;
        if (paused) StopAllSources();
    }

    public void SetStage(int stage) { currentStage = Mathf.Clamp(stage, 0, 3); }

    public void StartBGM()
    {
        StopBGM();
        bgmRoutine = StartCoroutine(BGMLoop());
    }

    public void StopBGM()
    {
        if (bgmRoutine != null) StopCoroutine(bgmRoutine);
        StopAllSources();
    }

    void StopAllSources()
    {
        kickSource.Stop(); hihatSource.Stop(); snareSource.Stop();
        bassSource.Stop(); arpSource.Stop();
    }

    //  BGM 시퀀서 
    IEnumerator BGMLoop()
    {
        float bpm = 130f;
        float step = 60f / bpm / 4f; // 16분음표
        int tick = 0;

        while (true)
        {
            if (muted || paused)
            {
                yield return new WaitForSecondsRealtime(step);
                tick++;
                continue;
            }

            int i = tick % 16;

            if (KICK_PATTERN[i] == 1)
                PlaySweep(kickSource, 140f, 45f, 0.25f, 1.0f, WaveType.Sine);

            if (HIHAT_PATTERN[i] == 1)
                PlayNoise(hihatSource, 0.04f, 8500f, 0.25f, FilterType.HighPass);

            if (currentStage >= 1 && SNARE_PATTERN[i] == 1)
                PlayNoise(snareSource, 0.08f, 1200f, 0.35f, FilterType.BandPass);

            if (i % 2 == 0)
            {
                float freq = BASS_NOTES[(i / 2) % BASS_NOTES.Length];
                if (currentStage >= 3) freq *= 2f;
                PlayTone(bassSource, freq, step * 1.7f, 0.5f, WaveType.Sawtooth);
            }

            if (currentStage >= 2 && tick % 2 == 0)
            {
                float freq = ARP_PATTERN[(tick / 2) % ARP_PATTERN.Length];
                PlayTone(arpSource, freq, step * 0.9f, 0.16f, WaveType.Triangle);
            }

            tick++;
            yield return new WaitForSecondsRealtime(step);
        }
    }

    //  효과음 
    public void PlayPlaceSound(int scoreIdx)
    {
        if (muted) return;
        float freq = SCALE[((scoreIdx % SCALE.Length) + SCALE.Length) % SCALE.Length];
        PlayTone(sfxSource, freq, 0.12f, 0.25f, WaveType.Square);
    }

    public void PlayPerfectSound(int streak)
    {
        if (muted) return;
        float baseFreq = 440f * Mathf.Pow(2f, (streak - 1) / 12f);
        StartCoroutine(PlayChord(baseFreq));
    }

    IEnumerator PlayChord(float baseFreq)
    {
        PlayTone(sfxSource, baseFreq, 0.18f, 0.35f, WaveType.Sine);
        yield return new WaitForSecondsRealtime(0.08f);
        PlayTone(sfxSource, baseFreq * 1.5f, 0.1f, 0.18f, WaveType.Sine);
    }

    public void PlayGameOverSound()
    {
        if (muted) return;
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        float[] notes = { 523.25f, 392f, 329.63f, 220f };
        for (int i = 0; i < notes.Length; i++)
        {
            PlayTone(sfxSource, notes[i], 0.3f, 0.25f, WaveType.Sawtooth);
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    //  파형 생성 코어 
    enum WaveType { Sine, Square, Sawtooth, Triangle }
    enum FilterType { HighPass, BandPass }

    void PlayTone(AudioSource src, float freq, float duration, float vol, WaveType type)
    {
        // 샘플 개수 = 샘플레이트 * 시간(초). 이만큼의 배열에 파형 데이터 채워넣을 거임
        int samples = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float phase = freq * t; // 몇 번째 사이클인지 (위상)
            float sample = GenerateWave(type, phase);
            // exp로 점점 작아지게 해서 끝부분 '뚝' 끊기는 노이즈 방지 (페이드아웃)
            float env = Mathf.Exp(-5f * t / duration);
            data[i] = sample * env * vol * masterVolume;
        }
        clip.SetData(data, 0);
        src.PlayOneShot(clip);
    }

    void PlaySweep(AudioSource src, float startFreq, float endFreq, float duration, float vol, WaveType type)
    {
        int samples = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("sweep", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        float phase = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            phase += freq / sampleRate;
            float sample = GenerateWave(type, phase);
            float env = Mathf.Exp(-6f * progress);
            data[i] = sample * env * vol * masterVolume;
        }
        clip.SetData(data, 0);
        src.PlayOneShot(clip);
    }

    void PlayNoise(AudioSource src, float duration, float cutoffFreq, float vol, FilterType filter)
    {
        int samples = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("noise", samples, 1, sampleRate, false);
        float[] data = new float[samples];

        // 화이트 노이즈 생성
        for (int i = 0; i < samples; i++)
            data[i] = Random.Range(-1f, 1f);

        // 간단한 1차 필터 적용
        if (filter == FilterType.HighPass)
        {
            float prev = data[0];
            float alpha = Mathf.Exp(-2f * Mathf.PI * cutoffFreq / sampleRate);
            for (int i = 1; i < samples; i++)
            {
                float cur = data[i];
                data[i] = alpha * (data[i - 1] + cur - prev);
                prev = cur;
            }
        }
        else // BandPass - 단순 이동평균 후 하이패스 흉내
        {
            float[] filtered = new float[samples];
            int window = Mathf.Max(1, sampleRate / (int)cutoffFreq);
            for (int i = 0; i < samples; i++)
            {
                float sum = 0f; int count = 0;
                for (int k = -window; k <= window; k++)
                {
                    int idx = i + k;
                    if (idx >= 0 && idx < samples) { sum += data[idx]; count++; }
                }
                filtered[i] = data[i] - (sum / count); // 평균 제거 = 고역 통과
            }
            data = filtered;
        }

        // 페이드아웃 적용
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-10f * t / duration);
            data[i] *= env * vol * masterVolume;
        }

        clip.SetData(data, 0);
        src.PlayOneShot(clip);
    }

    // 파형별 모양 계산 - p는 한 사이클 안에서 0~1 사이 위치
    float GenerateWave(WaveType type, float phase)
    {
        float p = phase - Mathf.Floor(phase); // 소수점만 남겨서 0~1로 정규화
        switch (type)
        {
            case WaveType.Sine:
                return Mathf.Sin(2f * Mathf.PI * phase); // 부드러운 파동
            case WaveType.Square:
                return p < 0.5f ? 1f : -1f; // 반은 +1 반은 -1, 8비트 느낌
            case WaveType.Sawtooth:
                return 2f * (p - 0.5f); // -1에서 1로 쭉 올라가다 뚝 끊김
            case WaveType.Triangle:
                return 1f - 4f * Mathf.Abs(Mathf.Round(p - 0.25f) - (p - 0.25f)); // 삼각형 모양
            default:
                return 0f;
        }
    }
}