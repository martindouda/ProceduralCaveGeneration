using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
    public void Visualize()
    {
        CaveGenerator.Instance.VisualizedSphere = int.Parse(gameObject.name.Substring(7));
        CaveGenerator.Instance.Visualize();
    }
}
