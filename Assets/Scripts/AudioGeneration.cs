using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AudioGeneration : MonoBehaviour
{
    [Range(4,2000)]
    public float frequency = 300;
    [Range(1,10)]
    public float amplitude;
    private float prevFrequency;
    private float prevAmplitude;
    public const int SAMPLE_RATE = 44100;
    private int audioBufferSize;
    private int graphBufferSize;
    private List<float> currWave;
    private List<float> refWave;
    private float currPhase;
    WaveGraph graph;
    AudioSource audioSource;
    int timeIndex = 0;
    private bool blended;
  // PRESENT
    public Dictionary<int, (float amplitude, float phase)> harmonics = new Dictionary<int, (float, float)>();
    public Dictionary<int, (float amplitude, float phase)> prevHarmonics = new Dictionary<int, (float, float)>();
    private bool playing;
    private bool harmed;
    private bool amped;
    private bool redrawn;
    private int reCalculationCounter;
    private const int reCalcMax = 120;
    private UI ui;
    private bool clipping;
    public GameObject clippingErrorText; 

    /**
    public enum WavePreset
    {
        STANDARD,
        SAW_WAVE,
        REVERSE_SAW_WAVE,
        TRIANGLE_WAVE,
        SQUARE_WAVE,
        RECTIFIED,
        PULSE
    }
    

    WavePreset currPreset;
    **/

    void Start()
    {
        //setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0; // force 2D sound
        audioSource.Stop(); // avoids audiosource from starting to play automatically

        //setup graph
        graph = GameObject.Find("Graph").GetComponent<WaveGraph>();
        graphBufferSize = SAMPLE_RATE / 8;
        float bufferRatio = (float)graphBufferSize / (float)SAMPLE_RATE;
        audioBufferSize = Mathf.RoundToInt(
            Mathf.Floor(frequency * bufferRatio) * ((float)SAMPLE_RATE / frequency));
        graph.setup(graphBufferSize);
        currWave = new List<float>(new float[graphBufferSize]);

        //setup initial values
        currPhase = 0;
        prevFrequency = frequency;
        amplitude = prevAmplitude = 10;
        playing = false;
        redrawn = false;
        clipping = false;
        reCalculationCounter = 0;
        harmonics.Add(1, (1, 0)); // add fundamental harmonic, with amplitude 1 and phase 0
        prevHarmonics = new Dictionary<int, (float, float)>(harmonics);
        currWave = WaveOperations.CreateSine(frequency, amplitude, currWave); // create initial wave
        //currPreset = WavePreset.STANDARD;

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
            float bufferRatio = (float)graphBufferSize / (float)SAMPLE_RATE;
            audioBufferSize = Mathf.RoundToInt(
                Mathf.Floor(frequency * bufferRatio) * ((float)SAMPLE_RATE / frequency));
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

        // Add harmonic on up press
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            /**
            int harmKey;
            if (harmonics.Count < 1) { harmKey = 1; }
            else { harmKey = harmonics.Keys.Max() + 1; }

            float tempAmplitude;
            float tempPhase;
            switch (currPreset)
            {
                case WavePreset.REVERSE_SAW_WAVE:
                    
                    tempAmplitude = (2f/Mathf.PI)/harmKey;
                    tempPhase = 1f/(harmKey * 2f);

                    harmonics.Add(harmKey, (tempAmplitude, tempPhase));
                    break;



                default:
                **/
                int harmKey = harmonics.Keys.Max() + 1;
                if (harmonics.Keys.Count > 0) 
                { 
                    harmonics.Add(harmKey, 
                    (1f / Mathf.Pow((float)harmKey, 2), 0));
                }
                else {harmonics.Add(1, (1f, 0f));}

                    //break;
            
        }

        // Remove harmonic on down press
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (harmonics.Count > 1) {harmonics.Remove(harmonics.Keys.Max());}
        }

        // Debug print harmonic values
        if (Input.GetKeyDown(KeyCode.X))
        {
            foreach (int harm in harmonics.Keys)
            {
                print(string.Format("{0} : {1}", harm, harmonics[harm]));
            }
        }

        // If user changes amplitude, execute reAmpSine
        if (amplitude != prevAmplitude)
        {
            currWave = WaveOperations.reAmpSine(amplitude / prevAmplitude, currWave);
            prevAmplitude = amplitude;
            amped = true;
        }

        // if harmonic added, execute addHarm for each new harmonic
        if (harmonics.Keys.Count > prevHarmonics.Keys.Count)
        {
            harmed = true;
            var newHarms = harmonics.Keys.Except(prevHarmonics.Keys);

            foreach (int newHarm in newHarms)
            {
                currWave = 
                WaveOperations.addHarm(frequency, amplitude, newHarm, harmonics[newHarm].amplitude, currPhase, currWave);
            }
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
        }

        // if harmonic amplitude changed, execute reAmpHarm for each adjusted harmonic
        else
        {
            foreach (int harm in harmonics.Keys)
            {
                float oldAmp = prevHarmonics[harm].amplitude;
                float newAmp = harmonics[harm].amplitude;

                if (oldAmp != newAmp)
                {
                    harmed = true;
                    float valChange = newAmp - oldAmp;
                    float harmPhase = (currPhase + harmonics[harm].phase) % 1f;
                    currWave = 
                    WaveOperations.reAmpHarm(frequency, amplitude, harm, valChange, harmPhase, currWave);
                }
                else
                {
                    float oldPhase = (currPhase + prevHarmonics[harm].phase) % 1f;
                    float newPhase = (currPhase + harmonics[harm].phase) % 1f;

                    if (oldPhase != newPhase)
                    {
                        harmed = true;
                        currWave = 
                        WaveOperations.rePhaseHarm(frequency, amplitude, harm, newAmp, oldPhase, newPhase, currWave);
                    }
                }
            }
        }
        
        // ensure prevHarmonics is updated to detect future changes and UI and graph are redrawn
        prevHarmonics = new Dictionary<int, (float amplitude, float phase)>(harmonics);

        if (harmed || amped)
        {
            if (playing) {graph.draw(currWave); redrawn = true;}
            ui.RedrawSliders(harmonics);
            harmed = amped = false;
        }
        // Safety net that ensures no artifacts remain present in the wave due to inaccuracies
        if (!redrawn && reCalculationCounter > reCalcMax && playing)
        {
            currWave = WaveOperations.CreateSine(frequency, amplitude, harmonics, currPhase, currWave);
            reCalculationCounter = 0;
            print("Recalc");
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
            data[i+1] = data[i] = currWave[timeIndex];
            timeIndex++;
        
            // reset index once it reaches audio buffer size
            if (timeIndex >= (audioBufferSize))
            {
                timeIndex = 0;
            }
            
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
}