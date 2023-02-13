using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphSlider : MonoBehaviour
{
    AudioGeneration audioGen;
    Slider slider;

    // Start is called before the first frame update
    void Start()
    {
        audioGen = GameObject.Find("Audio").GetComponent<AudioGeneration>();
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate {updateValue();});
    }

    void updateValue()
    {
        audioGen.amplitude = slider.value;
    }


}
