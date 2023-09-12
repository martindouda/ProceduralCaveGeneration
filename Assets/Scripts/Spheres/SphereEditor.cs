using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

// Class used to add special UI elements to Sphere component in unity's editor.
[CustomEditor(typeof(Sphere))]
public class SphereEditor : Editor
{
    // Adds a button to the UI.
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
