using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveGraph : MonoBehaviour
{
    public float yVal;
    public float zVal = 10;
    public float height;
    [Range(1,100)]
    public float zoom;
    private float prevZoom;
    private float ZOOM_CONST = 50; //simply a value to adjust the zoom sensitivity
    public GameObject graphPoint; //prefab for the points on the graph
    private List<GameObject> points = new List<GameObject>();
    private int bufferSize;
    List<float> currValues = new List<float>();

    public void setup(int size) 
    {
        bufferSize = size;

        for (int i = 0; i < bufferSize; i++)
        {
            GameObject point = Instantiate(graphPoint);
            point.transform.SetParent(transform, true);
            point.name = "GraphPoint" + i;
            points.Add(point);
        }
        draw(new List<float>(new float[bufferSize]));
        prevZoom = zoom = 1;
    }

    void Update()
    {
        if (zoom != prevZoom)
        {
            prevZoom = zoom;
            draw(currValues);
        }
    }

    public void draw(List<float> values)
    {
        currValues = values;
        // PRESENT
        for (int i = 0; i < values.Count; i++)
        {
            float x = (i - values.Count/2) * (zoom/ZOOM_CONST);
            if ((x < -80f || x > 80f))
            {
                points[i].SetActive(false);
            }
            else
            {
                points[i].transform.position = 
                new Vector3(x, yVal + values[i] * height, 10f);
                points[i].SetActive(true);
            }
        }
    }
}
