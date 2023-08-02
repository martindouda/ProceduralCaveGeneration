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
            if (GUILayout.Button("Generate", GUILayout.Width(100)))
            {
                caveGenerator.Generate();
            }
            GUILayout.FlexibleSpace();
            caveGenerator.VisualizedSphere = EditorGUILayout.IntField(caveGenerator.VisualizedSphere, GUILayout.Width(100));
            if (GUILayout.Button("Visualize", GUILayout.Width(100)))
            {
                caveGenerator.Visualize();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete", GUILayout.Width(100)))
            {
                caveGenerator.Pool.DeleteSpheres();
            }
            GUILayout.EndHorizontal();
            if (caveGenerator.GenerationTime > 0.0f) GUILayout.Label("Generation took: " + caveGenerator.GenerationTime + "ms");
            if (caveGenerator.VisualizationTime > 0.0f) GUILayout.Label("Visualization took: " + caveGenerator.VisualizationTime + "ms");
        }
    }
}
