/*
 * Project: Procedural Generation of Cave Systems
 * File: SphereEditor.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file provides a functionality to the editor to visualize the sphere's neighbours on a button click.
 */
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
