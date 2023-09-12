using UnityEngine;
using UnityEditor;

// Class used to add special UI elements to CaveGenerator component in unity's editor.
[CustomEditor(typeof(CaveGenerator))]
public class CaveGeneratorEditor : Editor
{
    // Adds 3 buttons to the UI.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CaveGenerator caveGenerator = (CaveGenerator)target;

        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate", GUILayout.Width(200)))
            {
                caveGenerator.Generate();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Visualize", GUILayout.Width(200)))
            {
                caveGenerator.Visualize();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete", GUILayout.Width(200)))
            {
                caveGenerator.Pool.DeleteSpheres();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            if (caveGenerator.GenerationTime > 0.0f) GUILayout.Label("Generation took: " + caveGenerator.GenerationTime * 1000.0f + "ms");
            if (caveGenerator.VisualizationTime > 0.0f) GUILayout.Label("Visualization took: " + caveGenerator.VisualizationTime * 1000.0f + "ms");
        }
    }
}
