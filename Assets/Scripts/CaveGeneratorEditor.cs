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
                caveGenerator.DeleteSpheres();
            }
            GUILayout.EndHorizontal();
            if (caveGenerator.GetGenerationTime() > 0.0f) GUILayout.Label("Generation took: " + caveGenerator.GetGenerationTime() + "ms");
        }
    }
}
