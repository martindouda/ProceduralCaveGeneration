using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
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
