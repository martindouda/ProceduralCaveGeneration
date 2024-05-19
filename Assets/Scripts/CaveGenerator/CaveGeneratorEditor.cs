/*
 * Project: Procedural Generation of Cave Systems
 * File: CaveGeneratorEditor.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This files contains all the buttons and some other options visible in CaveGenerators editor. It is used to control
 * the genration process either one by one or all at once. It is used to seperate the genration process into 5 stages: Poisson's spheres
 * generation, paths generation, cave mesh generation, speleothems generation and cave lakes generaion.
 */

using UnityEngine;
using UnityEditor;

// Class used to add special UI elements to CaveGenerator component in Unity's editor.
[CustomEditor(typeof(CaveGenerator))]
public class CaveGeneratorEditor : Editor
{
    private static bool m_ShowNeighbourToggle = false;
    private static bool m_RenderKeyPoints = false;
    private static bool m_RenderPathsToggle = false;
    private static bool m_RenderMeshToggle = true;
    private static bool m_RenderSpeleothems = true;
    private static bool m_RenderWater = true;

    private static int m_SelectedVisualizationOption = 0;
    private static string[] m_VisualizationOptions = { "Disabled", "Points On Path", "Spheres On Path", "All Spheres" };

    private static float m_GenerateSpheresDuration;
    private static float m_GeneratePathsDuration;
    private static float m_GenerateMeshDuration;
    private static float m_GenerateSpeleothemsDuration;
    private static float m_GenerateWaterDuration;

    //private static bool m_FirstRound = true;


    // ImGui style code which is automatically rendered in the CaveGenerator's inspector.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CaveGenerator caveGenerator = (CaveGenerator)target;


        GUILayout.Space(20);
        GUILayout.Label("DEBUG OPTIONS");
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
            EditorGUILayout.LabelField("Render Speleothems", GUILayout.Width(200));
            if (m_RenderSpeleothems != (m_RenderSpeleothems = EditorGUILayout.Toggle(m_RenderSpeleothems)))
            {
                caveGenerator.RenderSpeleothems(m_RenderSpeleothems);   
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        { 
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Water", GUILayout.Width(200));
            if (m_RenderWater != (m_RenderWater = EditorGUILayout.Toggle(m_RenderWater)))
            {
                caveGenerator.RenderWater(m_RenderWater);   
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
                caveGenerator.RenderPaths(m_RenderPathsToggle);
                m_GeneratePathsDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Mesh", GUILayout.Width(buttonWidth)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GenerateMesh();
                m_GenerateMeshDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Speleothems", GUILayout.Width(buttonWidth)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GenerateStalactites();
                m_GenerateSpeleothemsDuration = Time.realtimeSinceStartup - time;
            }
            if (GUILayout.Button("Generate Water", GUILayout.Width(buttonWidth)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GenerateWater();
                m_GenerateWaterDuration = Time.realtimeSinceStartup - time;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Everything", GUILayout.Width(200)))
            {
                float time = Time.realtimeSinceStartup;
                caveGenerator.GeneratePoissonSpheres();
                m_GenerateSpheresDuration = Time.realtimeSinceStartup - time;

                time = Time.realtimeSinceStartup;
                caveGenerator.GeneratePaths();
                m_GeneratePathsDuration = Time.realtimeSinceStartup - time; 

                time = Time.realtimeSinceStartup;
                caveGenerator.GenerateMesh();
                m_GenerateMeshDuration = Time.realtimeSinceStartup - time;

                time = Time.realtimeSinceStartup;
                caveGenerator.GenerateStalactites();
                m_GenerateSpeleothemsDuration = Time.realtimeSinceStartup - time;

                time = Time.realtimeSinceStartup;
                caveGenerator.GenerateWater();
                m_GenerateWaterDuration = Time.realtimeSinceStartup - time;

                CorrectButtons(caveGenerator);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();


            if (m_GenerateSpheresDuration > 0.0f) GUILayout.Label("Spheres took: " + m_GenerateSpheresDuration * 1000.0f + "ms");
            if (m_GeneratePathsDuration > 0.0f) GUILayout.Label("Paths took: " + m_GeneratePathsDuration * 1000.0f + "ms");
            if (m_GenerateMeshDuration > 0.0f) GUILayout.Label("Mesh took: " + m_GenerateMeshDuration * 1000.0f + "ms");
            if (m_GenerateSpeleothemsDuration > 0.0f) GUILayout.Label("Speleothems took: " + m_GenerateSpeleothemsDuration * 1000.0f + "ms");
            if (m_GenerateWaterDuration > 0.0f) GUILayout.Label("Water took: " + m_GenerateWaterDuration * 1000.0f + "ms");
        }
    }

    // Updates the CaveGenerator's state based on each button's state.
    private void CorrectButtons(CaveGenerator caveGenerator)
    {
        caveGenerator.RenderPoissonSpheres(m_SelectedVisualizationOption, m_ShowNeighbourToggle);
        caveGenerator.RenderKeyPoints(m_RenderKeyPoints);
        caveGenerator.RenderPaths(m_RenderPathsToggle);
        caveGenerator.RenderMesh(m_RenderMeshToggle);
        caveGenerator.RenderSpeleothems(m_RenderSpeleothems);
        caveGenerator.RenderWater(m_RenderWater);
    }
}
