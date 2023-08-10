using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CaveGenerator))]
public class CaveGeneratorEditor : Editor
{

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


            if (caveGenerator.GenerationTime > 0.0f) GUILayout.Label("Generation took: " + caveGenerator.GenerationTime + "ms");
            if (caveGenerator.VisualizationTime > 0.0f) GUILayout.Label("Visualization took: " + caveGenerator.VisualizationTime + "ms");
        }
    }
}
