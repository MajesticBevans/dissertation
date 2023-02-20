using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
public class Spinner : MonoBehaviour
{
    private bool phasing;
    private Image image;
    private float startMousexVal;
    private float startRotation;
    private float startValue;
    private Color turningColour = Color.red;
    private Color defaultColour = Color.yellow;
    public AudioGeneration audioGen;
    public int harmNum;
    private const float mouseScalar = 1.2f;
    public float currentValue;
    private const float DOUBLE_CLICK_DELAY = 0.5f;
    private float initial_click_time;
    void Start()
    {
        phasing = false;
        image = GetComponent<Image>();
        image.color = defaultColour;
        audioGen = GameObject.Find("Audio").GetComponent<AudioGeneration>();
        harmNum = int.Parse(gameObject.name.Remove(0,7));
        currentValue = audioGen.harmonics[harmNum].phase * harmNum;
        initial_click_time = 0;
    }

    void Update()
    {
        if (phasing)
        {
            if (Input.GetMouseButtonUp(0))
            {
                phasing = false;
                startValue = currentValue;
                image.color = defaultColour;
            }
            else
            {
                float scaledVal = mouseScalar * (Input.mousePosition.x - startMousexVal);
                float degreesVal = -scaledVal % 360;
                gameObject.transform.eulerAngles = new Vector3(0, 0, startRotation + degreesVal);

                updatePhaseValue(scaledVal / (360 * harmNum));
            }
        }
        else if (IsPointerOverSpinner())
        {
            if (Input.GetMouseButtonDown(0))
            {
                float now = Time.time;
                if (now > initial_click_time + DOUBLE_CLICK_DELAY)
                {
                    initial_click_time = now;
                    startValue = currentValue;
                    startMousexVal = Input.mousePosition.x;
                    startRotation = gameObject.transform.eulerAngles.z;
                    image.color = turningColour;
                    phasing = true;
                }
                else if (gameObject.transform.eulerAngles.z == 0)
                {
                    gameObject.transform.eulerAngles = new Vector3(0,0,180);
                    startValue = 0.5f * (1 / (float)harmNum);
                    updatePhaseValue(0);
                }
                else
                {
                    gameObject.transform.eulerAngles = new Vector3(0,0,0);
                    startValue = 0;
                    updatePhaseValue(0);
                }
            }

        }
    }

    private bool IsPointerOverSpinner()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            if (raycastResults[i].gameObject.name != gameObject.name)
            {
                raycastResults.RemoveAt(i);
                i--;
            }
        }

        return raycastResults.Count > 0;
    }

    private void updatePhaseValue(float value)
    {
        currentValue = (startValue + value) % 1f;
        audioGen.harmonics[harmNum] = (audioGen.harmonics[harmNum].amplitude, currentValue);
    }

    public void setValue(float value)
    {
        currentValue = value;
        transform.eulerAngles = new Vector3(0,0, value * 360);
    }
}