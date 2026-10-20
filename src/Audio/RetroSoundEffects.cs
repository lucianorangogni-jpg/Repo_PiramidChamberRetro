using System;
using Microsoft.Xna.Framework.Audio;

namespace RetroGamePiramid.Audio;

/// <summary>
/// Sintetizador y reproductor procedural de efectos de sonido arcade retro:
/// 1. Recolección de cofre de tesoro (fanfarria dorada egipcia brillante).
/// 2. Salto del arqueólogo (barrido ascendente arcade clásico).
/// 3. Pérdida de vida / eliminación (motivo descendente dramático).
/// Configurados con amplitud y volumen superiores a la música de fondo (DEFAULT_VOLUME = 1.0f)
/// y cero asignaciones en memoria heap (0 allocations) durante el bucle de juego.
/// </summary>
public sealed class RetroSoundEffects : IDisposable
{
    public const int SAMPLE_RATE = 22050;
    public const float DEFAULT_VOLUME = 1.0f;

    public const float TREASURE_DURATION_SECONDS = 0.44f;
    public const int TREASURE_TOTAL_SAMPLES = (int)(SAMPLE_RATE * TREASURE_DURATION_SECONDS);

    public const float JUMP_DURATION_SECONDS = 0.16f;
    public const int JUMP_TOTAL_SAMPLES = (int)(SAMPLE_RATE * JUMP_DURATION_SECONDS);

    public const float LOSE_LIFE_DURATION_SECONDS = 0.56f;
    public const int LOSE_LIFE_TOTAL_SAMPLES = (int)(SAMPLE_RATE * LOSE_LIFE_DURATION_SECONDS);

    private readonly SoundEffect? _treasureEffect;
    private readonly SoundEffectInstance? _treasureInstance;
    private readonly SoundEffect? _jumpEffect;
    private readonly SoundEffectInstance? _jumpInstance;
    private readonly SoundEffect? _loseLifeEffect;
    private readonly SoundEffectInstance? _loseLifeInstance;
    private bool _isMuted;
    private bool _isDisposed;

    public bool IsAudioAvailable => _treasureInstance != null;
    public bool IsMuted
    {
        get => _isMuted;
        set => _isMuted = value;
    }

    public RetroSoundEffects()
    {
        try
        {
            _treasureEffect = new SoundEffect(GenerateTreasurePcm16(SAMPLE_RATE), SAMPLE_RATE, AudioChannels.Mono);
            _treasureInstance = _treasureEffect.CreateInstance();
            _treasureInstance.IsLooped = false;
            _treasureInstance.Volume = DEFAULT_VOLUME;

            _jumpEffect = new SoundEffect(GenerateJumpPcm16(SAMPLE_RATE), SAMPLE_RATE, AudioChannels.Mono);
            _jumpInstance = _jumpEffect.CreateInstance();
            _jumpInstance.IsLooped = false;
            _jumpInstance.Volume = DEFAULT_VOLUME;

            _loseLifeEffect = new SoundEffect(GenerateLoseLifePcm16(SAMPLE_RATE), SAMPLE_RATE, AudioChannels.Mono);
            _loseLifeInstance = _loseLifeEffect.CreateInstance();
            _loseLifeInstance.IsLooped = false;
            _loseLifeInstance.Volume = DEFAULT_VOLUME;
        }
        catch (NoAudioHardwareException)
        {
            _treasureEffect = null;
            _treasureInstance = null;
            _jumpEffect = null;
            _jumpInstance = null;
            _loseLifeEffect = null;
            _loseLifeInstance = null;
        }
    }

    /// <summary>
    /// Reproduce el sonido especial de recolección de un cofre de tesoro.
    /// Cero asignaciones en heap (reutiliza SoundEffectInstance).
    /// </summary>
    public void PlayTreasure()
    {
        if (_isDisposed || _isMuted || _treasureInstance == null)
            return;

        if (_treasureInstance.State != SoundState.Stopped)
        {
            _treasureInstance.Stop();
        }
        _treasureInstance.Play();
    }

    /// <summary>
    /// Reproduce el sonido arcade de salto del arqueólogo.
    /// Cero asignaciones en heap (reutiliza SoundEffectInstance).
    /// </summary>
    public void PlayJump()
    {
        if (_isDisposed || _isMuted || _jumpInstance == null)
            return;

        if (_jumpInstance.State != SoundState.Stopped)
        {
            _jumpInstance.Stop();
        }
        _jumpInstance.Play();
    }

    /// <summary>
    /// Reproduce el sonido dramático al perder una vida.
    /// Cero asignaciones en heap (reutiliza SoundEffectInstance).
    /// </summary>
    public void PlayLoseLife()
    {
        if (_isDisposed || _isMuted || _loseLifeInstance == null)
            return;

        if (_loseLifeInstance.State != SoundState.Stopped)
        {
            _loseLifeInstance.Stop();
        }
        _loseLifeInstance.Play();
    }

    /// <summary>
    /// Genera las muestras normalizadas (-1.0 a 1.0) del sonido de recolección de tesoro:
    /// fanfarria brillante de 4 notas ascendentes (D5 -> F#5 -> A5 -> D6) con timbre rico y cristalino.
    /// </summary>
    public static float[] GenerateTreasureSamples(int sampleRate = SAMPLE_RATE)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 8000);

        int totalSamples = (int)(sampleRate * TREASURE_DURATION_SECONDS);
        float[] samples = new float[totalSamples];

        // Arpegio triunfal: Re5 (587.33 Hz) -> Fa#5 (739.99 Hz) -> La5 (880.00 Hz) -> Re6 (1174.66 Hz)
        ReadOnlySpan<float> notes = [587.33f, 739.99f, 880.00f, 1174.66f];
        ReadOnlySpan<float> durations = [0.08f, 0.08f, 0.08f, 0.20f];

        float phase = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;

            float freq = notes[3];
            float localTime = t - 0.24f;
            float noteDur = durations[3];

            if (t < 0.08f)
            {
                freq = notes[0];
                localTime = t;
                noteDur = durations[0];
            }
            else if (t < 0.16f)
            {
                freq = notes[1];
                localTime = t - 0.08f;
                noteDur = durations[1];
            }
            else if (t < 0.24f)
            {
                freq = notes[2];
                localTime = t - 0.16f;
                noteDur = durations[2];
            }

            phase += MathF.Tau * freq / sampleRate;

            // Envolvente percusiva brillante con caída limpia
            float attack = Math.Clamp(localTime / 0.006f, 0f, 1f);
            float decay = 1f - 0.45f * Math.Clamp(localTime / noteDur, 0f, 1f);
            if (t >= 0.24f)
            {
                float tailProgress = Math.Clamp((t - 0.24f) / 0.20f, 0f, 1f);
                decay = 1f - (tailProgress * tailProgress);
            }

            // Onda cuadrada suave + octava de campana dorada
            float wave = MathF.Sin(phase) * 0.62f +
                         MathF.Sin(phase * 2f) * 0.24f +
                         MathF.Sin(phase * 3f) * 0.14f;

            samples[i] = Math.Clamp(wave * attack * decay * 0.94f, -0.96f, 0.96f);
        }

        return samples;
    }

    /// <summary>
    /// Genera las muestras normalizadas (-1.0 a 1.0) del sonido de salto:
    /// barrido ascendente rápido de frecuencia (200 Hz -> 580 Hz) estilo arcade 8-bit/16-bit.
    /// </summary>
    public static float[] GenerateJumpSamples(int sampleRate = SAMPLE_RATE)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 8000);

        int totalSamples = (int)(sampleRate * JUMP_DURATION_SECONDS);
        float[] samples = new float[totalSamples];

        float phase = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            float progress = (float)i / totalSamples;
            // Curva ascendente acelerada de 200 Hz a 580 Hz
            float freq = 200f + 380f * (progress * progress);
            phase += MathF.Tau * freq / sampleRate;

            float env = 1f;
            if (progress < 0.06f)
            {
                env = progress / 0.06f;
            }
            else if (progress > 0.78f)
            {
                env = Math.Clamp((1f - progress) / 0.22f, 0f, 1f);
            }

            // Onda retro con armónicos impares (tipo pulso/cuadrada suave)
            float wave = MathF.Sin(phase) * 0.68f +
                         MathF.Sin(phase * 3f) * 0.22f +
                         MathF.Sin(phase * 5f) * 0.10f;

            samples[i] = Math.Clamp(wave * env * 0.92f, -0.96f, 0.96f);
        }

        return samples;
    }

    /// <summary>
    /// Genera las muestras normalizadas (-1.0 a 1.0) del sonido al perder una vida:
    /// caída cromática/frigia dramática de 4 escalones (La4 -> Sol4 -> Fa#4 -> Mib4 -> Re3).
    /// </summary>
    public static float[] GenerateLoseLifeSamples(int sampleRate = SAMPLE_RATE)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 8000);

        int totalSamples = (int)(sampleRate * LOSE_LIFE_DURATION_SECONDS);
        float[] samples = new float[totalSamples];

        ReadOnlySpan<float> baseNotes = [440.00f, 392.00f, 369.99f, 293.66f];
        float stepDuration = LOSE_LIFE_DURATION_SECONDS / 4f; // 0.14s por escalón

        float phase = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            int step = Math.Min((int)(t / stepDuration), 3);
            float localTime = t - (step * stepDuration);
            float localProgress = Math.Clamp(localTime / stepDuration, 0f, 1f);

            // Ligero descenso de tono dentro de cada nota y caída grave en la última
            float bend = step == 3 ? (1f - 0.35f * localProgress) : (1f - 0.08f * localProgress);
            float freq = baseNotes[step] * bend;
            phase += MathF.Tau * freq / sampleRate;

            float env = 1f;
            if (localProgress < 0.08f)
            {
                env = localProgress / 0.08f;
            }
            else if (step == 3 && localProgress > 0.55f)
            {
                env = Math.Clamp((1f - localProgress) / 0.45f, 0f, 1f);
            }

            // Timbre denso de sierra/cuadrada retro para destacar claramente la pérdida de vida
            float wave = MathF.Sin(phase) * 0.58f +
                         MathF.Sin(phase * 2f) * 0.24f +
                         MathF.Sin(phase * 3f) * 0.18f;

            samples[i] = Math.Clamp(wave * env * 0.95f, -0.96f, 0.96f);
        }

        return samples;
    }

    public static byte[] GenerateTreasurePcm16(int sampleRate = SAMPLE_RATE) =>
        ConvertSamplesToPcm16(GenerateTreasureSamples(sampleRate));

    public static byte[] GenerateJumpPcm16(int sampleRate = SAMPLE_RATE) =>
        ConvertSamplesToPcm16(GenerateJumpSamples(sampleRate));

    public static byte[] GenerateLoseLifePcm16(int sampleRate = SAMPLE_RATE) =>
        ConvertSamplesToPcm16(GenerateLoseLifeSamples(sampleRate));

    private static byte[] ConvertSamplesToPcm16(float[] samples)
    {
        byte[] pcmBytes = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short pcmValue = (short)Math.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
            pcmBytes[i * 2] = (byte)(pcmValue & 0xFF);
            pcmBytes[(i * 2) + 1] = (byte)((pcmValue >> 8) & 0xFF);
        }
        return pcmBytes;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _treasureInstance?.Stop();
            _treasureInstance?.Dispose();
            _treasureEffect?.Dispose();

            _jumpInstance?.Stop();
            _jumpInstance?.Dispose();
            _jumpEffect?.Dispose();

            _loseLifeInstance?.Stop();
            _loseLifeInstance?.Dispose();
            _loseLifeEffect?.Dispose();

            _isDisposed = true;
        }
    }
}

