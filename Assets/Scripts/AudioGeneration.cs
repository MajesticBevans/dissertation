using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AudioGeneration : MonoBehaviour
{
    public enum WavePreset
    {
        N_SQUARED_FALLOFF,
        N_FALLOFF,
        SAW_WAVE,
        REVERSE_SAW_WAVE,
        TRIANGLE_WAVE,
        SQUARE_WAVE,
    }

    // CONSTS
    public const int SAMPLE_RATE = 44100;
    private const int reCalcMax = 60;
    private const float AMPLITUDE_MAX = 10f;
    private const float FREQUENCY_MAX = 500f;
    private const float FREQUENCY_MIN = 8f;

    // IMPORTS
    WaveGraph graph;
    AudioSource audioSource;
    private UI ui;
    public GameObject clippingErrorText;  

    // WAVE PARAMETERS
    [Range(FREQUENCY_MIN,FREQUENCY_MAX)]
    public float frequency;
    [Range(0,AMPLITUDE_MAX)]
    public float amplitude;
    private float currPhase;
    private float prevFrequency;
    private float prevAmplitude;
    private int audioBufferSize;
    private int graphBufferSize;
    private float bufferRatio;
    public Dictionary<int, (float amplitude, float phase)> harmonics = new Dictionary<int, (float, float)>();
    public Dictionary<int, (float amplitude, float phase)> prevHarmonics = new Dictionary<int, (float, float)>();
    private List<float> currWave;
    private WavePreset currPreset;
    int timeIndex = 0;

    // CHANGE DETECTION BOOLS
    private bool blended;
    private bool playing;
    private bool harmed;
    private bool amped;
    private bool redrawn;
    private bool clipping;
    private bool preset_changed;
    private int reCalculationCounter;   
    

    void Start()
    {
        //setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0; // force 2D sound
        audioSource.Stop(); // avoids audiosource from starting to play automatically

        //setup graph
        graph = GameObject.Find("Graph").GetComponent<WaveGraph>();
        graphBufferSize = SAMPLE_RATE / (int)FREQUENCY_MIN;
        bufferRatio = (float)graphBufferSize / (float)SAMPLE_RATE;
        audioBufferSize = Mathf.RoundToInt(
            Mathf.Floor(frequency * bufferRatio) * ((float)SAMPLE_RATE / frequency));
        graph.setup(graphBufferSize);
        currWave = new List<float>(new float[graphBufferSize]);

        //setup initial values
        currPhase = 0;
        frequency = 200;
        prevFrequency = frequency;
        amplitude = prevAmplitude = AMPLITUDE_MAX;
        playing = false;
        redrawn = false;
        clipping = false;
        reCalculationCounter = 0;
        harmonics.Add(1, (1, 0)); // add fundamental harmonic, with amplitude 1 and phase 0
        prevHarmonics = new Dictionary<int, (float, float)>(harmonics);
        WaveOperations.setSampleRate(SAMPLE_RATE);
        currWave = WaveOperations.CreateSine(frequency, amplitude, currWave); // create initial wave
        currPreset = WavePreset.N_FALLOFF;

        //setup UI
        ui = GameObject.Find("Canvas").GetComponent<UI>();
        ui.RedrawSliders(harmonics);
        clippingErrorText.SetActive(false);
    }

    void Update()
    {
        redrawn = false;
        // ensures graph is redrawn and audio loop point is reset when frequency
        // is adjusted by user
        if (blended)
        {
            blended = false;
            graph.draw(currWave);
            redrawn = true;

            // PRESENT
            bufferRatio = (float)graphBufferSize / (float)SAMPLE_RATE;
            audioBufferSize = Mathf.RoundToInt(Mathf.Floor(frequency * bufferRatio) * ((float)SAMPLE_RATE / frequency));
        }
        
        if (clipping && !clippingErrorText.activeSelf)
        {
            clippingErrorText.SetActive(true);
        }
        else if (!clipping && clippingErrorText.activeSelf)
        {
            clippingErrorText.SetActive(false);
        }
        
        // Play/Pause audio
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!audioSource.isPlaying)
            {
                timeIndex = 0;  // resets timer before playing sound
                currWave = WaveOperations.CreateSine(frequency, amplitude, harmonics, currPhase, currWave);
                audioSource.Play();
                graph.draw(currWave);
                redrawn = true;
                playing = true;
            }
            else
            {
                audioSource.Stop();
                graph.draw(new List<float>(new float[graphBufferSize]));
                currPhase = 0;
                //TODO implement elegant stop
                playing = false;
            }
        }

        // Quit key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        // Add harmonic on up press
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            addHarm();
        }

        // Remove harmonic on down press
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            removeHarm();
        }

        // If user changes amplitude, execute reAmpSine
        if (amplitude != prevAmplitude)
        {
            currWave = WaveOperations.reAmpSine(amplitude / prevAmplitude, currWave);
            prevAmplitude = amplitude;
            amped = true;
        }

        // If preset has changed, recalculate entire wave
        if (preset_changed)
        {
            prevHarmonics = new Dictionary<int, (float amplitude, float phase)>();

            foreach (int harm in harmonics.Keys)
            {
                prevHarmonics[harm] = HandlePreset(harm);
            }

            WaveOperations.CreateSine(frequency, amplitude, prevHarmonics, currPhase, currWave);
            harmonics = new Dictionary<int, (float amplitude, float phase)>(prevHarmonics);
            harmed = true;
            preset_changed = false;
            // ensure prevHarmonics is updated to detect future changes and UI and graph are redrawn
            prevHarmonics = new Dictionary<int, (float amplitude, float phase)>(harmonics);
        }

        // if harmonic added, execute addHarm for each new harmonic
        else if (harmonics.Keys.Count > prevHarmonics.Keys.Count)
        {
            harmed = true;
            var newHarms = harmonics.Keys.Except(prevHarmonics.Keys);

            foreach (int newHarm in newHarms)
            {
                float harmPhase = (currPhase + harmonics[newHarm].phase) % 1f;
                currWave = 
                WaveOperations.addHarm(frequency, amplitude, newHarm, harmonics[newHarm].amplitude, harmPhase, currWave);
            }
            // ensure prevHarmonics is updated to detect future changes and UI and graph are redrawn
            prevHarmonics = new Dictionary<int, (float amplitude, float phase)>(harmonics);
        }

        // if harmonic removed, execute removeHarm for each removed harmonic
        else if (prevHarmonics.Keys.Count > harmonics.Keys.Count)
        {
            harmed = true;
            var oldHarms = prevHarmonics.Keys.Except(harmonics.Keys);

            foreach (int oldHarm in oldHarms)
            {
                float harmPhase = (currPhase + prevHarmonics[oldHarm].phase) % 1f;
                currWave = 
                WaveOperations.removeHarm(frequency, amplitude, oldHarm, prevHarmonics[oldHarm].amplitude, harmPhase, currWave);
            }
            // ensure prevHarmonics is updated to detect future changes and UI and graph are redrawn
            prevHarmonics = new Dictionary<int, (float amplitude, float phase)>(harmonics);
        }

        // detect amplitude/phase changes per harmonic
        else
        {
            foreach (int harm in harmonics.Keys)
            {
                float oldAmp = prevHarmonics[harm].amplitude;
                float newAmp = harmonics[harm].amplitude;

                float oldPhase = (currPhase + prevHarmonics[harm].phase) % (1f / harm);
                float newPhase = (currPhase + harmonics[harm].phase) % (1f / harm);

                if (oldAmp != newAmp && oldPhase != newPhase) print("detected");

                if (oldAmp != newAmp)
                {
                    harmed = true;
                    float valChange = newAmp - oldAmp;
                    currWave = 
                    WaveOperations.reAmpHarm(frequency, amplitude, harm, valChange, oldPhase, currWave);
                }
                if (oldPhase != newPhase)
                {
                    harmed = true;
                    currWave = 
                    WaveOperations.rePhaseHarm(frequency, amplitude, harm, newAmp, oldPhase, newPhase, currWave);
                }
            }
            // ensure prevHarmonics is updated to detect future changes and UI and graph are redrawn
            prevHarmonics = new Dictionary<int, (float amplitude, float phase)>(harmonics);
        }

        if (harmed || amped)
        {
            if (playing) {graph.draw(currWave); redrawn = true;}
            ui.RedrawSliders(harmonics);
            harmed = amped = false;
        }
        
        // Safety net that ensures no artifacts remain present in the wave due to inaccuracies
        if (!redrawn && reCalculationCounter > reCalcMax && playing)
        {
            List<float> tempWave = new List<float>(currWave);
            currWave = WaveOperations.CreateSine(frequency, amplitude, harmonics, currPhase, currWave);

            if (!WavesEqual(currWave, tempWave))
            {
                graph.draw(currWave);
            }
            reCalculationCounter = 0;
        }
        else
        {
            reCalculationCounter++;
        }
    }

    // Takes a small buffer of samples from currWave and passes them to the audio filter
    void OnAudioFilterRead(float[] data, int channels)
    {
        clipping = false;

        // WRITE NEXT CHUNK TO AUDIO BUFFER (512 samples)
        for (int i = 0; i < data.Length; i+= channels)
        {  
            // feed data to both audio channels (L and R)
            data[i+1] = data[i] = currWave[timeIndex] / 2; // divided by 2 to control volume
            timeIndex++;
        
            // reset index once it reaches audio buffer size
            if (timeIndex >= (audioBufferSize))
            {
                timeIndex = 0;
            }
            
            // if any values are over 1, then clipping will occur
            if (data[i] > 1)
            {
                clipping = true;
            }
        }

        // If user changes frequency, execute BlendSine
        if (frequency != prevFrequency)
        {
            if (playing)
            {
                currPhase = 
                WaveOperations.BlendSine(frequency, prevFrequency, amplitude, harmonics, currPhase, timeIndex, currWave);
            }
            else
            {
                currWave = WaveOperations.CreateSine(frequency, amplitude, harmonics, currPhase, currWave);
            }

            timeIndex = 0;
            prevFrequency = frequency;
            blended = true;
        }
    }

    public void addHarm()
    {
        int harmKey = harmonics.Count < 1 ? 1 : harmonics.Keys.Max() + 1;

        if (harmKey == 2 && currPreset == WavePreset.REVERSE_SAW_WAVE) harmonics[1] = (harmonics[1].amplitude, 0.5f);
        else if (harmKey == 2) harmonics[1] = (harmonics[1].amplitude, 0f);

        harmonics[harmKey] = HandlePreset(harmKey);
    }

    public void removeHarm() { if (harmonics.Count > 1) harmonics.Remove(harmonics.Keys.Max()); }

    public (float, float) HandlePreset(int harmKey)
    {
        float tempAmplitude;
        float tempPhase;

        switch (currPreset)
        {
            case WavePreset.SAW_WAVE:
                tempAmplitude = Mathf.Pow(-1f, (float)harmKey) / (float)harmKey;

                if (tempAmplitude < 0)
                {
                    tempAmplitude *= -1f;
                    tempPhase = 0;
                }
                else { tempPhase = 1f / (2f * (float)harmKey); }
                break;

            case WavePreset.REVERSE_SAW_WAVE:
                tempAmplitude = Mathf.Pow(-1f, (float)harmKey) / (float)harmKey;

                if (tempAmplitude < 0)
                {
                    tempAmplitude *= -1f;
                    tempPhase = 1f / (2f * (float)harmKey);
                }
                else { tempPhase = 0; }
                break;

            case WavePreset.TRIANGLE_WAVE:
                if (harmKey % 2 == 0)
                {
                    tempAmplitude = 0;
                    tempPhase = 0;
                }
                else
                {
                    tempAmplitude = 1f/ Mathf.Pow((float)harmKey, 2f);
                    tempPhase = (harmKey - 1) % 4 == 0 ? 0 : 1f / (2f * (float)harmKey);
                }
                break;

            case WavePreset.SQUARE_WAVE:
                tempAmplitude = harmKey % 2 == 0 ? 0 : 1f/(float)harmKey;
                tempPhase = 0;
                break;

            case WavePreset.N_FALLOFF:
                tempAmplitude = 1f / (float)harmKey;
                tempPhase = 0;
                break;

            case WavePreset.N_SQUARED_FALLOFF:
                tempAmplitude = 1f / Mathf.Pow((float)harmKey, 2);
                tempPhase = 0;
                break;
            
            default:
                tempAmplitude = 1f; 
                tempPhase = 0f;
                break;
        }
        return (tempAmplitude, tempPhase);
    }

    public void ChangePreset(WavePreset new_preset)
    {
        if (currPreset != new_preset)
        {
            currPreset = new_preset;
            preset_changed = true;
        }
        prevHarmonics = new Dictionary<int, (float amplitude, float phase)>(harmonics);
    }

    bool WavesEqual(List<float> wave1, List<float> wave2)
    {
        if (wave1.Count != wave2.Count) { return false; }

        for (int i = 0; i < wave1.Count; i++)
        {
            if (wave2[i] != wave1[i]) { return false; }
        }
        return true;
    }
}