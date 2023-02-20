using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownHandler : MonoBehaviour
{
    private Dictionary<string, AudioGeneration.WavePreset> preset_translation = 
    new Dictionary<string, AudioGeneration.WavePreset>();
    AudioGeneration audioGen;

    // Start is called before the first frame update
    void Start()
    {
        Dropdown dropdown;
        dropdown = transform.GetComponent<Dropdown>();
        audioGen = GameObject.Find("Audio").GetComponent<AudioGeneration>();

        dropdown.onValueChanged.AddListener(delegate {DropdownSelect(dropdown); });

        preset_translation.Add("N Falloff", AudioGeneration.WavePreset.N_FALLOFF);
        preset_translation.Add("N-Squared Falloff", AudioGeneration.WavePreset.N_SQUARED_FALLOFF);
        preset_translation.Add("Saw Wave", AudioGeneration.WavePreset.SAW_WAVE);
        preset_translation.Add("Reverse Saw Wave", AudioGeneration.WavePreset.REVERSE_SAW_WAVE);
        preset_translation.Add("Triangle Wave", AudioGeneration.WavePreset.TRIANGLE_WAVE);
        preset_translation.Add("Square Wave", AudioGeneration.WavePreset.SQUARE_WAVE);

    }

    void DropdownSelect(Dropdown dropdown)
    {
        audioGen.ChangePreset(preset_translation[dropdown.options[dropdown.value].text]);
    }
}
