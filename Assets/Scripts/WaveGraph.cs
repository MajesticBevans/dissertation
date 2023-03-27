using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveGraph : MonoBehaviour
{
    public const float yVal = -25f;
    public float zVal = 10;
    public const float height = 35;
    [Range(1,50)]
    public float zoom;
    private float prevZoom;
    private float ZOOM_CONST = 45; //simply a value to adjust the zoom sensitivity
    public GameObject graphPoint; //prefab for the points on the graph
    private List<GameObject> points = new List<GameObject>();
    private int bufferSize;
    private const float graph_bounds = 65f;
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
            float x = transform.position.x + (i - values.Count/2) * (zoom/ZOOM_CONST);
            if ((x < transform.position.x - graph_bounds || x > transform.position.x + graph_bounds))
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