using System;
using RetroGamePiramid.Audio;
using Xunit;

namespace RetroGamePiramid.Tests.Audio;

public class EgyptianAmbientMusicTests
{
    [Fact]
    public void GenerateAmbientSamples_ReturnsExactSampleCount_AndSoftAmplitudeRange()
    {
        float[] samples = EgyptianAmbientMusic.GenerateAmbientSamples();

        Assert.NotNull(samples);
        Assert.Equal(EgyptianAmbientMusic.TOTAL_SAMPLES, samples.Length);

        float maxAbs = 0f;
        double sumSquares = 0;

        for (int i = 0; i < samples.Length; i++)
        {
            float abs = MathF.Abs(samples[i]);
            if (abs > maxAbs)
            {
                maxAbs = abs;
            }
            sumSquares += samples[i] * samples[i];
        }

        double rms = Math.Sqrt(sumSquares / samples.Length);

        // Debe tener señal claramente audible sin saturar
        Assert.True(rms > 0.06, $"El nivel RMS ({rms:F4}) debe ser claramente audible");
        Assert.True(maxAbs <= 0.96f, $"El pico máximo ({maxAbs:F4}) debe mantenerse sin saturación");

        // Continuidad suave entre el final y el inicio del bucle para evitar clics al repetir
        float loopBoundaryDelta = MathF.Abs(samples[^1] - samples[0]);
        Assert.True(loopBoundaryDelta < 0.25f, $"La transición del bucle ({loopBoundaryDelta:F4}) debe ser suave");
    }

    [Fact]
    public void GenerateAmbientPcm16_ReturnsValid16BitLittleEndianBuffer()
    {
        byte[] pcmBytes = EgyptianAmbientMusic.GenerateAmbientPcm16();

        Assert.NotNull(pcmBytes);
        Assert.Equal(EgyptianAmbientMusic.TOTAL_SAMPLES * 2, pcmBytes.Length);

        bool hasNonZeroSample = false;
        for (int i = 0; i < pcmBytes.Length; i += 2)
        {
            short sample = (short)(pcmBytes[i] | (pcmBytes[i + 1] << 8));
            if (sample != 0)
            {
                hasNonZeroSample = true;
                break;
            }
        }

        Assert.True(hasNonZeroSample, "El buffer PCM de 16 bits debe contener muestras de audio sintetizadas");
    }

    [Fact]
    public void RetroSoundEffects_TreasureJumpAndLoseLife_AreLouderThanAmbientMusic_AndValidPcm16()
    {
        // El volumen por defecto de los efectos (1.0f) es superior al de la música de fondo (0.55f)
        Assert.True(RetroSoundEffects.DEFAULT_VOLUME > EgyptianAmbientMusic.DEFAULT_VOLUME);

        float[] ambientSamples = EgyptianAmbientMusic.GenerateAmbientSamples();
        float[] treasureSamples = RetroSoundEffects.GenerateTreasureSamples();
        float[] jumpSamples = RetroSoundEffects.GenerateJumpSamples();
        float[] loseLifeSamples = RetroSoundEffects.GenerateLoseLifeSamples();

        Assert.Equal(RetroSoundEffects.TREASURE_TOTAL_SAMPLES, treasureSamples.Length);
        Assert.Equal(RetroSoundEffects.JUMP_TOTAL_SAMPLES, jumpSamples.Length);
        Assert.Equal(RetroSoundEffects.LOSE_LIFE_TOTAL_SAMPLES, loseLifeSamples.Length);

        double ambientRms = ComputeRms(ambientSamples) * EgyptianAmbientMusic.DEFAULT_VOLUME;
        double treasureRms = ComputeRms(treasureSamples) * RetroSoundEffects.DEFAULT_VOLUME;
        double jumpRms = ComputeRms(jumpSamples) * RetroSoundEffects.DEFAULT_VOLUME;
        double loseLifeRms = ComputeRms(loseLifeSamples) * RetroSoundEffects.DEFAULT_VOLUME;

        // Los 3 efectos de sonido deben tener mayor energía efectiva (RMS * volumen) que el sonido de fondo
        Assert.True(treasureRms > ambientRms * 1.5, $"El sonido de tesoro ({treasureRms:F3}) debe escucharse más fuerte que el fondo ({ambientRms:F3})");
        Assert.True(jumpRms > ambientRms * 1.5, $"El sonido de salto ({jumpRms:F3}) debe escucharse más fuerte que el fondo ({ambientRms:F3})");
        Assert.True(loseLifeRms > ambientRms * 1.5, $"El sonido de perder vida ({loseLifeRms:F3}) debe escucharse más fuerte que el fondo ({ambientRms:F3})");

        Assert.Equal(treasureSamples.Length * 2, RetroSoundEffects.GenerateTreasurePcm16().Length);
        Assert.Equal(jumpSamples.Length * 2, RetroSoundEffects.GenerateJumpPcm16().Length);
        Assert.Equal(loseLifeSamples.Length * 2, RetroSoundEffects.GenerateLoseLifePcm16().Length);
    }

    private static double ComputeRms(float[] samples)
    {
        double sumSq = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sumSq += samples[i] * samples[i];
        }
        return Math.Sqrt(sumSq / samples.Length);
    }
}

