using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoomSlider : MonoBehaviour
{
    WaveGraph graph;
    Slider slider;

    // Start is called before the first frame update
    void Start()
    {
        graph = GameObject.Find("Graph").GetComponent<WaveGraph>();
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate {updateValue();});
    }

    void updateValue()
    {
        graph.zoom = slider.value;
    }


}
