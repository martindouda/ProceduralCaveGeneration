using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class used to visualize the spheres connected to this sphere.
public class Sphere : MonoBehaviour
{
    // Visualizes the spheres connected to this sphere.
    public void Visualize()
    {
        if (CaveGenerator.Instance == null)
        {
            Debug.LogWarning("Update on CaveGenerator is necessary!");
            return;
        }
        CaveGenerator.Instance.VisualizedSphere = int.Parse(gameObject.name.Substring(7));
        CaveGenerator.Instance.Visualize();
    }
}
