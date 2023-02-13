using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
public class UI : MonoBehaviour
{
    // Prefabs
    public GameObject amp_slider_prefab;
    public GameObject phase_spinner_prefab;
    public GameObject frequency_slider_prefab;
    public GameObject text_prefab;

    // Position offsets and constants
    private float last_xVal;
    private const float AMP_YVAL = 140f;
    private const float PHASE_SPINNER_OFFSET = 80f;
    private const float NEW_HARM_OFFSET = 40f;
    private const float TEXT_OFFSET = 75f;

    // Harmonic parameter dictionaries
    public Dictionary<int, Slider> amp_sliders = new Dictionary<int, Slider>();
    public Dictionary<int, GameObject> phase_spinners = new Dictionary<int, GameObject>();
    public Dictionary<int, Text> harm_labels = new Dictionary<int, Text>();

    // Script imports
    public AudioGeneration audioGen;
    public WaveGraph waveGraph;

    void Start()
    {
        // Initialise starting harmonic parameters position
        last_xVal = -485;

        // Import scripts
        audioGen = GameObject.Find("Audio").GetComponent<AudioGeneration>();
        waveGraph = GameObject.Find("Graph").GetComponent<WaveGraph>();
    }

    public void RedrawSliders(Dictionary<int, (float amplitude, float phase)> harms)
    {
        if (harms.Keys.Count > amp_sliders.Keys.Count)
        {
            var diff = harms.Keys.Except(amp_sliders.Keys);

            foreach (int newHarm in diff)
            {
                GameObject text_object = 
                Instantiate(text_prefab,
                new Vector3(last_xVal + NEW_HARM_OFFSET, AMP_YVAL + TEXT_OFFSET, 11), 
                Quaternion.identity);

                GameObject amp_slider_object = 
                Instantiate(amp_slider_prefab, 
                new Vector3(last_xVal + NEW_HARM_OFFSET, AMP_YVAL, 11), 
                Quaternion.identity);

                GameObject phase_spinner_object =
                Instantiate(phase_spinner_prefab,
                new Vector3(last_xVal + NEW_HARM_OFFSET, AMP_YVAL - PHASE_SPINNER_OFFSET, 11),
                Quaternion.identity);

                text_object.name = "HarmLabel" + newHarm;
                amp_slider_object.name = "Amp" + newHarm.ToString();
                phase_spinner_object.name = "Spinner" + newHarm.ToString();
                text_object.transform.SetParent(transform, false);
                amp_slider_object.transform.SetParent(transform, false);
                phase_spinner_object.transform.SetParent(transform, false);

                Text text = text_object.GetComponent<Text>();
                text.text = newHarm.ToString();
                text.alignment = TextAnchor.MiddleCenter;
                Slider amp_slider = amp_slider_object.GetComponent<Slider>();
                amp_slider.onValueChanged.AddListener (delegate {ChangeAmpSliderValue (newHarm);});

                harm_labels.Add(newHarm, text);
                amp_sliders.Add(newHarm, amp_slider);
                amp_sliders[newHarm].value = harms[newHarm].amplitude;
                phase_spinners.Add(newHarm, phase_spinner_object);

                last_xVal += NEW_HARM_OFFSET;
            }
        }
        else if (harms.Keys.Count < amp_sliders.Keys.Count)
        {
            var diff = amp_sliders.Keys.Except(harms.Keys);

            foreach (int oldHarm in diff)
            {
                Destroy(harm_labels[oldHarm].gameObject);
                harm_labels.Remove(oldHarm);
                Destroy(amp_sliders[oldHarm].gameObject);
                amp_sliders.Remove(oldHarm);
                Destroy(phase_spinners[oldHarm]);
                phase_spinners.Remove(oldHarm);

                last_xVal -= NEW_HARM_OFFSET;
            }
        }
    }

    public void ChangeAmpSliderValue(int harm)
    {
        audioGen.harmonics[harm] = (amp_sliders[harm].value, audioGen.harmonics[harm].phase);
    }
}
