using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FrequencySlider : MonoBehaviour
{
    AudioGeneration audioGen;
    Slider slider;
    Text label;
    // Start is called before the first frame update
    void Start()
    {
        audioGen = GameObject.Find("Audio").GetComponent<AudioGeneration>();
        slider = GetComponent<Slider>();
        label = GameObject.Find("FrequencyLabel").GetComponent<Text>();
        slider.onValueChanged.AddListener(delegate {updateValue();});
    }

    void updateValue()
    {
        audioGen.frequency = slider.value;
        label.text = "Frequency [" + slider.value.ToString() + "]";
    }


}
