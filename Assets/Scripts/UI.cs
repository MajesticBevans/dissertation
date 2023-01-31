using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
public class UI : MonoBehaviour
{
    public GameObject amp_slider_prefab;
    public GameObject phase_spinner_prefab;
    public GameObject text_prefab;
    private float last_xVal;
    private const float amp_yVal = 140f;
    private const float phase_spinner_offset = 100f;
    private const float new_harm_offset = 40f;
    private const float text_offset = 75;
    public Dictionary<int, Slider> amp_sliders = new Dictionary<int, Slider>();
    public Dictionary<int, GameObject> phase_spinners = new Dictionary<int, GameObject>();
    public Dictionary<int, Text> harm_labels = new Dictionary<int, Text>();
    public AudioGeneration audioGen;

    void Start()
    {
        last_xVal = -490;
        audioGen = GameObject.Find("Audio").GetComponent<AudioGeneration>();
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
                new Vector3(last_xVal + new_harm_offset + text_offset, amp_yVal + text_offset, 11), 
                Quaternion.identity);

                GameObject amp_slider_object = 
                Instantiate(amp_slider_prefab, 
                new Vector3(last_xVal + new_harm_offset, amp_yVal, 11), 
                Quaternion.identity);

                GameObject phase_spinner_object =
                Instantiate(phase_spinner_prefab,
                new Vector3(last_xVal + new_harm_offset, amp_yVal - phase_spinner_offset, 11),
                Quaternion.identity);

                text_object.name = "HarmLabel" + newHarm;
                amp_slider_object.name = "Amp" + newHarm.ToString();
                phase_spinner_object.name = "Spinner" + newHarm.ToString();
                text_object.transform.SetParent(transform, false);
                amp_slider_object.transform.SetParent(transform, false);
                phase_spinner_object.transform.SetParent(transform, false);

                Text text = text_object.GetComponent<Text>();
                text.text = newHarm.ToString();
                Slider amp_slider = amp_slider_object.GetComponent<Slider>();
                amp_slider.onValueChanged.AddListener (delegate {ChangeAmpSliderValue (newHarm);});

                harm_labels.Add(newHarm, text);
                amp_sliders.Add(newHarm, amp_slider);
                amp_sliders[newHarm].value = harms[newHarm].amplitude;
                phase_spinners.Add(newHarm, phase_spinner_object);

                last_xVal += new_harm_offset;
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

                last_xVal -= new_harm_offset;
            }
        }
    }

    public void ChangeAmpSliderValue(int harm)
    {
        audioGen.harmonics[harm] = (amp_sliders[harm].value, audioGen.harmonics[harm].phase);
    }
}
