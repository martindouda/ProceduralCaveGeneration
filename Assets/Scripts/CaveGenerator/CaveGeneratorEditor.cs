using UnityEngine;
using UnityEditor;

// Class used to add special UI elements to CaveGenerator component in unity's editor.
[CustomEditor(typeof(CaveGenerator))]
public class CaveGeneratorEditor : Editor
{
    private bool m_ShowNeighbourToggle = false;
    private bool m_RenderPathsToggle = false;
    private bool m_RenderMeshToggle = false;

    private int m_SelectedVisualizationOption = 0;
    private string[] m_VisualizationOptions = { "Disabled", "Points On Path", "Spheres On Path", "All Spheres" };
    private float m_TransparencySlider = 1.0f;

    private float m_GenerateSpheresDuration;
    private float m_GeneratePathsDuration;
    private float m_GenerateMeshDuration;

    private bool m_FirstRound;

    private void Awake()
    {
        m_FirstRound = true;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CaveGenerator caveGenerator = (CaveGenerator)target;

        if (m_FirstRound)
        {
            m_SelectedVisualizationOption = 0;
            m_ShowNeighbourToggle = false;
            caveGenerator.RenderPoissonSpheres(m_SelectedVisualizationOption, m_ShowNeighbourToggle);
            m_RenderPathsToggle = false;
            caveGenerator.RenderPaths(m_RenderPathsToggle);

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
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Spheres", GUILayout.Width(150)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GeneratePoissonSpheres();
                m_GenerateSpheresDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Paths", GUILayout.Width(150)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GeneratePaths();
                m_GeneratePathsDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Mesh", GUILayout.Width(150)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GenerateMesh();
                m_GenerateMeshDuration = Time.realtimeSinceStartup - time;
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
