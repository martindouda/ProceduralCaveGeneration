using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

[CustomEditor(typeof(Sphere))]
public class SphereEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Sphere sphere = target as Sphere;

        if (GUILayout.Button("Visualize connected"))
        {
            sphere.Visualize();
        }
    }
}
