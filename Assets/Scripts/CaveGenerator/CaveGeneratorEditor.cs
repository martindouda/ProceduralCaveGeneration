using UnityEngine;
using UnityEditor;

// Class used to add special UI elements to CaveGenerator component in unity's editor.
[CustomEditor(typeof(CaveGenerator))]
public class CaveGeneratorEditor : Editor
{
    private static bool m_ShowNeighbourToggle = false;
    private static bool m_RenderKeyPoints = false;
    private static bool m_RenderPathsToggle = false;
    private static bool m_RenderMeshToggle = true;
    private static bool m_RenderStalactites = true;

    private static int m_SelectedVisualizationOption = 0;
    private static string[] m_VisualizationOptions = { "Disabled", "Points On Path", "Spheres On Path", "All Spheres" };
    private static float m_TransparencySlider = 1.0f;

    private static float m_GenerateSpheresDuration;
    private static float m_GeneratePathsDuration;
    private static float m_GenerateMeshDuration;

    private static bool m_FirstRound = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CaveGenerator caveGenerator = (CaveGenerator)target;

        if (m_FirstRound)
        {
            Debug.Log("First Round");
            m_SelectedVisualizationOption = 0;
            m_ShowNeighbourToggle = false;
            m_RenderPathsToggle = false;
            m_RenderMeshToggle = true;

            if (caveGenerator.CheckSpheresDistributionReady())
            {
                caveGenerator.RenderPoissonSpheres(m_SelectedVisualizationOption, m_ShowNeighbourToggle);
                if (caveGenerator.CheckPathsGenerated())
                {
                    caveGenerator.RenderPaths(m_RenderPathsToggle);
                }
            }

            m_FirstRound = false;
        }


        GUILayout.Space(20);
        GUILayout.Label("CUSTOM EDITOR OPTIONS");
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Poisson Spheres", GUILayout.Width(200));
            if (m_SelectedVisualizationOption != (m_SelectedVisualizationOption = EditorGUILayout.Popup(m_SelectedVisualizationOption, m_VisualizationOptions)))
            {
                if (!caveGenerator.CheckSpheresDistributionReady())
                {
                    m_SelectedVisualizationOption = 0;
                }
                else
                {
                    caveGenerator.RenderPoissonSpheres(m_SelectedVisualizationOption, m_ShowNeighbourToggle);
                }
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (m_SelectedVisualizationOption == 3)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Show Neighbour", GUILayout.Width(200));
                if (m_ShowNeighbourToggle != (m_ShowNeighbourToggle = EditorGUILayout.Toggle(m_ShowNeighbourToggle)))
                {
                    if (!caveGenerator.CheckSpheresDistributionReady() || !caveGenerator.CheckPathsGenerated())
                    {
                        m_ShowNeighbourToggle = !m_ShowNeighbourToggle;
                    }
                    else
                    {
                        caveGenerator.RenderPoissonSpheres(m_SelectedVisualizationOption, m_ShowNeighbourToggle);
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Sphere Transparency", GUILayout.Width(200));
                if (m_TransparencySlider != (m_TransparencySlider = EditorGUILayout.Slider(m_TransparencySlider, 0.0f, 1.0f)))
                {
                    caveGenerator.SetTransparency(m_TransparencySlider);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
        { 
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Key Points", GUILayout.Width(200));
            if (m_RenderKeyPoints != (m_RenderKeyPoints = EditorGUILayout.Toggle(m_RenderKeyPoints)))
            {
                caveGenerator.RenderKeyPoints(m_RenderKeyPoints);   
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Paths", GUILayout.Width(200));
            if (m_RenderPathsToggle != (m_RenderPathsToggle = EditorGUILayout.Toggle(m_RenderPathsToggle)))
            {
                if (!caveGenerator.CheckSpheresDistributionReady() || !caveGenerator.CheckPathsGenerated())
                {
                    m_RenderPathsToggle = !m_RenderPathsToggle; 
                }
                else
                {
                    caveGenerator.RenderPaths(m_RenderPathsToggle);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        { 
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Mesh", GUILayout.Width(200));
            if (m_RenderMeshToggle != (m_RenderMeshToggle = EditorGUILayout.Toggle(m_RenderMeshToggle)))
            {
                caveGenerator.RenderMesh(m_RenderMeshToggle);   
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        { 
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Render Stalactites", GUILayout.Width(200));
            if (m_RenderStalactites != (m_RenderStalactites = EditorGUILayout.Toggle(m_RenderStalactites)))
            {
                caveGenerator.RenderStalactites(m_RenderStalactites);   
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        {
            int buttonWidth = 125;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Spheres", GUILayout.Width(buttonWidth)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GeneratePoissonSpheres();
                m_GenerateSpheresDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Paths", GUILayout.Width(buttonWidth)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GeneratePaths();
                m_GeneratePathsDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Mesh", GUILayout.Width(buttonWidth)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GenerateMesh();
                m_GenerateMeshDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Stalactites", GUILayout.Width(buttonWidth)))
            {
                caveGenerator.GenerateStalactites();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete", GUILayout.Width(200)))
            {
                caveGenerator.Pool.DeleteSpheres();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();


            if (m_GenerateSpheresDuration > 0.0f) GUILayout.Label("Spheres took: " + m_GenerateSpheresDuration * 1000.0f + "ms");
            if (m_GeneratePathsDuration > 0.0f) GUILayout.Label("Paths took: " + m_GeneratePathsDuration * 1000.0f + "ms");
            if (m_GenerateMeshDuration > 0.0f) GUILayout.Label("Mesh took: " + m_GenerateMeshDuration * 1000.0f + "ms");
        }
    }
}
