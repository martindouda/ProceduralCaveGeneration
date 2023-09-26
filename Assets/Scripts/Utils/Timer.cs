using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer
{
    private string title;
    private float startTime;

    public Timer(string title)
    {
        this.title = title;
        startTime = Time.realtimeSinceStartup;
    }

    public void End()
    {
        Debug.Log(title + " took: " + (Time.realtimeSinceStartup - startTime) * 1000.0f + "ms to finnish.");
    }
}
