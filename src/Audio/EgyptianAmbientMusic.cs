using System;
using Microsoft.Xna.Framework.Audio;

namespace RetroGamePiramid.Audio;

/// <summary>
/// Sintetizador y reproductor procedural de música ambiental egipcia suave para las recámaras de la pirámide.
/// Utiliza la escala Hijaz / Frigia Dominante (Re - Mi♭ - Fa♯ - Sol - La - Si♭ - Do - Re) con timbre suave
/// de flauta antigua (nay), arpegio de laúd (oud), bordón cálido de piedra y eco de cámara sin costuras.
/// Genera el buffer PCM de 16 bits una sola vez al cargar y garantiza 0 asignaciones en heap durante Update y Draw.
/// </summary>
public sealed class EgyptianAmbientMusic : IDisposable
{
    public const int SAMPLE_RATE = 22050;
    public const int TOTAL_STEPS = 32;
    public const float STEP_DURATION_SECONDS = 0.5f; // 16 segundos por ciclo completo
    public const int TOTAL_SAMPLES = (int)(SAMPLE_RATE * TOTAL_STEPS * STEP_DURATION_SECONDS);
    public const float DEFAULT_VOLUME = 0.55f;

    // Frecuencias de la escala egipcia Hijaz (Re Frigio Dominante)
    private const float NOTE_REST = 0f;
    private const float NOTE_D2 = 73.42f;
    private const float NOTE_A2 = 110.00f;
    private const float NOTE_D3 = 146.83f;
    private const float NOTE_EB3 = 155.56f;
    private const float NOTE_FS3 = 185.00f;
    private const float NOTE_G3 = 196.00f;
    private const float NOTE_A3 = 220.00f;
    private const float NOTE_BB3 = 233.08f;
    private const float NOTE_C4 = 261.63f;
    private const float NOTE_D4 = 293.66f;
    private const float NOTE_EB4 = 311.13f;
    private const float NOTE_FS4 = 369.99f;
    private const float NOTE_G4 = 392.00f;
    private const float NOTE_A4 = 440.00f;
    private const float NOTE_BB4 = 466.16f;
    private const float NOTE_C5 = 523.25f;
    private const float NOTE_D5 = 587.33f;

    // Melodía principal suave (32 pasos de 0.5s = 16s) en escala Hijaz
    private static readonly float[] LeadMelodyNotes =
    {
        // Frase 1: Ascenso místico por el intervalo característico Re -> Mib -> Fa# -> Sol -> La
        NOTE_D4,  NOTE_D4,  NOTE_EB4, NOTE_FS4,
        NOTE_G4,  NOTE_FS4, NOTE_EB4, NOTE_D4,
        // Frase 2: Respuesta contemplativa con reposo en La3 y Re4
        NOTE_EB4, NOTE_D4,  NOTE_C4,  NOTE_A3,
        NOTE_D4,  NOTE_D4,  NOTE_REST, NOTE_REST,
        // Frase 3: Elevación hacia el registro medio-alto (Sol -> La -> Sib -> La -> Fa#)
        NOTE_G4,  NOTE_A4,  NOTE_BB4, NOTE_A4,
        NOTE_FS4, NOTE_G4,  NOTE_A4,  NOTE_FS4,
        // Frase 4: Descenso hipnótico de regreso a la tónica Re
        NOTE_G4,  NOTE_FS4, NOTE_EB4, NOTE_D4,
        NOTE_EB4, NOTE_D4,  NOTE_D4,  NOTE_REST
    };

    // Acompañamiento suave de arpa/laúd egipcio (oud) en registro grave-medio
    private static readonly float[] OudArpeggioNotes =
    {
        NOTE_D3,  NOTE_A3,  NOTE_D4,  NOTE_A3,
        NOTE_G3,  NOTE_D3,  NOTE_EB3, NOTE_A3,
        NOTE_C4,  NOTE_G3,  NOTE_A3,  NOTE_EB3,
        NOTE_D3,  NOTE_A2,  NOTE_D3,  NOTE_A3,

        NOTE_G3,  NOTE_D4,  NOTE_BB3, NOTE_G3,
        NOTE_FS3, NOTE_D3,  NOTE_A3,  NOTE_FS3,
        NOTE_EB3, NOTE_A3,  NOTE_G3,  NOTE_EB3,
        NOTE_D3,  NOTE_A2,  NOTE_D3,  NOTE_REST
    };

    private readonly SoundEffect? _soundEffect;
    private readonly SoundEffectInstance? _instance;
    private bool _isMuted;
    private bool _isDisposed;

    public bool IsAudioAvailable => _instance != null;
    public bool IsMuted => _isMuted;
    public bool IsPlaying => _instance is { State: SoundState.Playing };

    public float Volume
    {
        get => _instance?.Volume ?? DEFAULT_VOLUME;
        set
        {
            if (_instance != null)
            {
                _instance.Volume = Math.Clamp(value, 0f, 1f);
            }
        }
    }

    public EgyptianAmbientMusic()
    {
        try
        {
            byte[] pcmBytes = GenerateAmbientPcm16(SAMPLE_RATE);
            _soundEffect = new SoundEffect(pcmBytes, SAMPLE_RATE, AudioChannels.Mono);
            _instance = _soundEffect.CreateInstance();
            _instance.IsLooped = true;
            _instance.Volume = DEFAULT_VOLUME;
        }
        catch (NoAudioHardwareException)
        {
            // Entorno sin dispositivo de audio (p. ej. CI o sistema sin salida de sonido): degradación elegante
            _soundEffect = null;
            _instance = null;
        }
    }

    /// <summary>
    /// Inicia o reanuda la reproducción en bucle si no está silenciada.
    /// Libre de asignaciones en memoria heap (0 allocations).
    /// </summary>
    public void Play()
    {
        if (_isDisposed || _instance == null || _isMuted)
            return;

        if (_instance.State != SoundState.Playing)
        {
            _instance.Play();
        }
    }

    /// <summary>
    /// Pausa la música ambiental (por ejemplo, al volver al menú principal).
    /// Libre de asignaciones en memoria heap (0 allocations).
    /// </summary>
    public void Pause()
    {
        if (_isDisposed || _instance == null)
            return;

        if (_instance.State == SoundState.Playing)
        {
            _instance.Pause();
        }
    }

    /// <summary>
    /// Alterna el estado de silencio (Mute) de la música de fondo.
    /// </summary>
    public void ToggleMute()
    {
        _isMuted = !_isMuted;
        if (_instance == null || _isDisposed)
            return;

        if (_isMuted)
        {
            if (_instance.State == SoundState.Playing)
            {
                _instance.Pause();
            }
        }
        else
        {
            _instance.Play();
        }
    }

    /// <summary>
    /// Genera las muestras normalizadas (-1.0 a 1.0) de la melodía ambiental egipcia.
    /// Método puro y determinista desacoplado del hardware de audio para pruebas unitarias.
    /// </summary>
    public static float[] GenerateAmbientSamples(int sampleRate = SAMPLE_RATE)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 8000);

        int samplesPerStep = (int)(sampleRate * STEP_DURATION_SECONDS);
        int totalSamples = samplesPerStep * TOTAL_STEPS;
        float[] dryBuffer = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float time = (float)i / sampleRate;
            int stepIndex = i / samplesPerStep;
            if (stepIndex >= TOTAL_STEPS)
            {
                stepIndex = TOTAL_STEPS - 1;
            }

            // 1. Bordón místico de fondo (quinta justa suave D2 + A2 que evoca la resonancia de la pirámide)
            float droneMod = 0.85f + 0.15f * MathF.Sin(MathF.Tau * 0.125f * time);
            float drone = (MathF.Sin(MathF.Tau * NOTE_D2 * time) * 0.65f +
                           MathF.Sin(MathF.Tau * NOTE_A2 * time) * 0.35f) * 0.12f * droneMod;

            // 2. Melodía principal con timbre suave de flauta egipcia (nay) y ligadura entre notas iguales
            float leadNote = LeadMelodyNotes[stepIndex];
            float leadSample = 0f;
            if (leadNote > 0f)
            {
                // Determinar si la nota se mantiene ligada con el paso anterior o siguiente para suavizar el fraseo
                bool tiedPrev = stepIndex > 0 && MathF.Abs(LeadMelodyNotes[stepIndex - 1] - leadNote) < 0.01f;
                bool tiedNext = stepIndex < TOTAL_STEPS - 1 && MathF.Abs(LeadMelodyNotes[stepIndex + 1] - leadNote) < 0.01f;

                int sampleInStep = i - (stepIndex * samplesPerStep);
                float stepProgress = (float)sampleInStep / samplesPerStep;

                float env = 1f;
                if (!tiedPrev && stepProgress < 0.18f)
                {
                    env = stepProgress / 0.18f;
                }
                else if (!tiedNext && stepProgress > 0.72f)
                {
                    env = Math.Clamp((1f - stepProgress) / 0.28f, 0f, 1f);
                }

                // Suave curva sinusoidal en la envolvente para evitar clics
                env = env * env * (3f - 2f * env);

                // Vibrato orgánico muy sutil (4.5 Hz)
                float vibrato = 1f + 0.0035f * MathF.Sin(MathF.Tau * 4.5f * time);
                float freq = leadNote * vibrato;

                // Armónicos suaves filtrados (fundamental + 2do armónico cálido + 3er armónico tenue)
                float fundamental = MathF.Sin(MathF.Tau * freq * time);
                float harmonic2 = MathF.Sin(MathF.Tau * (freq * 2f) * time) * 0.22f;
                float harmonic3 = MathF.Sin(MathF.Tau * (freq * 3f) * time) * 0.08f;

                leadSample = (fundamental + harmonic2 + harmonic3) * env * 0.30f;
            }

            // 3. Arpegio suave de laúd/arpa antigua (oud) con decaimiento exponencial suave
            float oudNote = OudArpeggioNotes[stepIndex];
            float oudSample = 0f;
            if (oudNote > 0f)
            {
                int sampleInStep = i - (stepIndex * samplesPerStep);
                float stepTime = (float)sampleInStep / sampleRate;
                float attack = Math.Clamp(stepTime / 0.015f, 0f, 1f);
                float decay = MathF.Exp(-stepTime * 6.5f);
                float pluckEnv = attack * decay;

                float oudFundamental = MathF.Sin(MathF.Tau * oudNote * time);
                float oudHarmonic2 = MathF.Sin(MathF.Tau * (oudNote * 2f) * time) * 0.30f;
                oudSample = (oudFundamental + oudHarmonic2) * pluckEnv * 0.20f;
            }

            dryBuffer[i] = drone + leadSample + oudSample;
        }

        // 4. Eco acústico de cámara de piedra con envoltura circular (loop-around) para bucle infinito sin clics
        float[] wetBuffer = new float[totalSamples];
        int delaySamples1 = (int)(sampleRate * 0.375f); // Eco a 375 ms (corchea con puntillo)
        int delaySamples2 = (int)(sampleRate * 0.750f); // Segundo eco tenue a 750 ms

        for (int i = 0; i < totalSamples; i++)
        {
            int idx1 = (i - delaySamples1 + totalSamples) % totalSamples;
            int idx2 = (i - delaySamples2 + totalSamples) % totalSamples;

            float mixed = dryBuffer[i] + (dryBuffer[idx1] * 0.32f) + (dryBuffer[idx2] * 0.14f);
            wetBuffer[i] = Math.Clamp(mixed, -0.95f, 0.95f);
        }

        return wetBuffer;
    }

    /// <summary>
    /// Convierte las muestras sintetizadas en un arreglo de bytes PCM de 16 bits Little-Endian listo para SoundEffect.
    /// </summary>
    public static byte[] GenerateAmbientPcm16(int sampleRate = SAMPLE_RATE)
    {
        float[] samples = GenerateAmbientSamples(sampleRate);
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
            _instance?.Stop();
            _instance?.Dispose();
            _soundEffect?.Dispose();
            _isDisposed = true;
        }
    }
}

