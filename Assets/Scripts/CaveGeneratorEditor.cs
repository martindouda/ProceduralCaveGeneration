using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CaveGenerator))]
public class CaveGeneratorEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CaveGenerator caveGenerator = (CaveGenerator)target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate"))
        {
            caveGenerator.Generate();
        }
        if (GUILayout.Button("Delete"))
        {
            caveGenerator.Delete();
        }
        GUILayout.EndHorizontal();

        if (caveGenerator.GetGenerationTime() > 0.0f) GUILayout.Label("Generation took: " + caveGenerator.GetGenerationTime() + "ms");
    }
}
