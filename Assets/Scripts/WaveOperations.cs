using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WaveOperations
{
    private static int SAMPLE_RATE;
    private const float AMPLITUDE_SCALE_FACTOR = 0.01f;
    private const float twoPi = 2f * Mathf.PI;
    public static void setSampleRate(int rate)
    {
        SAMPLE_RATE = rate;
    }

    // Creates a sinewave with phase=0
    public static List<float> CreateSine(float frequency, float amplitude, List<float> wave)
    {
        amplitude *= AMPLITUDE_SCALE_FACTOR;

        for (int i = 0; i < wave.Count; i++)
        {
            wave[i] = amplitude * Mathf.Sin(twoPi * i * frequency / SAMPLE_RATE);
        }
        return wave;
    }
    
    // Creates a sinewave with phase=0, but allowing harmonics with phase=0
    public static List<float> CreateSine(float frequency, float amplitude, Dictionary<int, (float amplitude, float phase)> harms, List<float> wave)
    {
        amplitude *= AMPLITUDE_SCALE_FACTOR;

        for (int i = 0; i < wave.Count; i++)
        {
            float value = 0;

            foreach (int key in harms.Keys)
            {
                value += amplitude * harms[key].amplitude * Mathf.Sin(twoPi * i * key * frequency / SAMPLE_RATE);
            }
            wave[i] = value;
        }
        return wave;
    }

    // Creates a sinewave as above, but allowing harmonics w/ phase and accounting for current fundamental phase
    public static List<float> CreateSine(float frequency, float amplitude, Dictionary<int, (float amplitude, float phase)> harms, float phase, List<float> wave)
    {
        amplitude *= AMPLITUDE_SCALE_FACTOR;

        for (int i = 0; i < wave.Count; i++)
        {
            float value = 0;

            foreach (int key in harms.Keys)
            {
                float harmPhase = (phase + harms[key].phase) % 1f;
                value += amplitude * harms[key].amplitude * Mathf.Sin(twoPi * key * ((i * frequency / SAMPLE_RATE) - harmPhase));
            }
            wave[i] = value;
        }
        return wave;
    }

    // Calculate the phase of the wave beginning at the y position at the end of the current
    // audioFilterBuffer
    // Redraw entire wave with new phase
    // reset time index 
    public static float BlendSine(float frequency, float prevFrequency, float amplitude, Dictionary<int,(float amplitude, float phase)> harms, float wavePhase, int index, List<float> wave)
    {
        float waveProgress = index * prevFrequency / SAMPLE_RATE;
        amplitude *= AMPLITUDE_SCALE_FACTOR;

        wavePhase -= waveProgress;

        for (int i = 0; i < wave.Count; i++)
        {
            float value = 0;

            foreach (int key in harms.Keys)
            {
                float harmPhase = (wavePhase + harms[key].phase);
                value += amplitude * harms[key].amplitude * Mathf.Sin(twoPi * key * ((i * frequency / SAMPLE_RATE) - harmPhase));
            }
            wave[i] = value;
        }
        return wavePhase;
    }

    // Multiply every value in the wave by the ratio of new amplitude / old amplitude 
    public static List<float> reAmpSine(float ampRatio, List<float> wave)
    {
        for (int i = 0; i < wave.Count; i++)
        {
            wave[i] *= ampRatio;
        }
        return wave;
    }

    // Add the harmonic with frequency harm * fundamental frequency to the wave
    public static List<float> addHarm(float frequency, float amplitude, int harm, float harmAmplitude, float phase, List<float> wave)
    {
        amplitude *= AMPLITUDE_SCALE_FACTOR;
        for (int i = 0; i < wave.Count; i++)
        {
            wave[i] += amplitude * harmAmplitude * Mathf.Sin(twoPi * harm * ((i  * frequency / SAMPLE_RATE) - phase));
        }
        return wave;
    }

    // Remove the harmonic with frequency harm * fundamental frequency from the wave
    public static List<float> removeHarm(float frequency, float amplitude, int harm, float harmAmplitude, float harmPhase, List<float> wave)
    {
        amplitude *= AMPLITUDE_SCALE_FACTOR;
        for (int i = 0; i < wave.Count; i++)
        {
            wave[i] -= amplitude * harmAmplitude * Mathf.Sin(twoPi * harm * ((i  * frequency / SAMPLE_RATE) - harmPhase));
        }
        return wave;
    }

    // Change the amplitude of the harmonic with frequency harm * fundamental frequency
    // by subtracting the harmonic with the old amplitude from each point of the graph,
    // and then adding the harmonic back with the new amplitude
    public static List<float> reAmpHarm(float frequency, float amplitude, int harm, float valChange, float harmPhase, List<float> wave)
    {
        amplitude *= AMPLITUDE_SCALE_FACTOR;
        
        for (int i = 0; i < wave.Count; i++)
        {
            wave[i] += amplitude * valChange * Mathf.Sin(twoPi * harm * ((i  * frequency / SAMPLE_RATE) - harmPhase));
        }
        return wave;
    }

    public static List<float> rePhaseHarm(float frequency, float amplitude, int harm, float ampHarm, float oldPhase, float newPhase, List<float> wave)
    {
        amplitude *= AMPLITUDE_SCALE_FACTOR;

        for (int i = 0; i < wave.Count; i++)
        {
            wave[i] -= amplitude * ampHarm * Mathf.Sin(twoPi * harm * ((i * frequency / SAMPLE_RATE) - oldPhase));
            wave[i] += amplitude * ampHarm * Mathf.Sin(twoPi * harm * ((i * frequency / SAMPLE_RATE) - newPhase));
        }
        return wave;
    }
}
