/*
 * Project: Procedural Generation of Cave Systems
 * File: Sphere.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file provides a functionality to the spheres to visualize their neighbours.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class used to visualize the spheres connected to this sphere.
public class Sphere : MonoBehaviour
{
    // Visualizes the spheres connected to this sphere.
    public void Visualize()
    {
        Debug.LogWarning("This feature has been disabled.");
        /*if (CaveGenerator.Instance == null)
        {
            Debug.LogWarning("Update on CaveGenerator is necessary!");
            return;
        }
        CaveGenerator.Instance.VisualizedSphere = int.Parse(gameObject.name.Substring(7));*/
    }
}
