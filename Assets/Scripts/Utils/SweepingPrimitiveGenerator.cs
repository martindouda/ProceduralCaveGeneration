/*
 * Project: Procedural Generation of Cave Systems
 * File: SweepingPrimitiveGenerator.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file provides functionality for generating primitive shapes such as beds, tubes, keyholes, canyons, and passages based on
 * specified textures. Additionally, it utilizes Poisson Discs distribution to distribute points within these shapes, considering factors such
 * as verticality and distance from the water table.
*/
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using static PoissonSpheres;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

public class SweepingPrimitiveGenerator : MonoBehaviour
{
    [SerializeField] private Texture2D m_ImageBed; private Color[] m_PixelsBed;
    [SerializeField] private Texture2D m_ImageTube; private Color[] m_PixelsTube;
    [SerializeField] private Texture2D m_ImageKeyhole; private Color[] m_PixelsKeyhole;
    [SerializeField] private Texture2D m_ImageCanyon; private Color[] m_PixelsCanyon;
    [SerializeField] private Texture2D m_ImagePassage; private Color[] m_PixelsPassage;

    [SerializeField] private int m_SamplesBeforeRejection = 20;

    [SerializeField, Range(0.0f, 1.0f)] private float m_VerticalityCoefficient = 0.707f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_DistanceFromWaterTableKeyhole = 0.3f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_DistanceFromWaterTableCanyon = 0.6f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_DistanceFromWaterTablePassage = 0.5f;


    private Vector2Int m_GridSize;
    private float m_CellSize;
    private Color[] pixels;
    

    // Loads the pixels from the cave images.
    public void LoadPixels()
    {
        m_PixelsBed = m_ImageBed.GetPixels();
        m_PixelsTube = m_ImageTube.GetPixels();
        m_PixelsKeyhole = m_ImageKeyhole.GetPixels();
        m_PixelsCanyon = m_ImageCanyon.GetPixels();
        m_PixelsPassage = m_ImagePassage.GetPixels();
    }

    /*[SerializeField] private Transform m_Prefab;
    [SerializeField] private float m_Radius = 0.5f;

    private void Start()
    {
        LoadPixels();
        GeneratePoints(Vector3.forward, 0.0f, 3.0f, m_Radius);
    }*/

    // Performs the Poisson Discs distribution among the pixels and returns a list of the resulting disc positions. 
    public List<Vector3> GeneratePoints(Vector3 tangent, float yNormalized, float primitiveRadius, float discRadius)
    {
        Vector2 regionSize = new Vector2(primitiveRadius, primitiveRadius) * 2.0f;
        float verticality = Mathf.Abs(tangent.y);
        if (verticality > m_VerticalityCoefficient)
        {
            if (yNormalized > m_DistanceFromWaterTablePassage)          pixels = m_PixelsPassage;
            else                                                        pixels = m_PixelsTube;
        }
        else
        {
            if (yNormalized >= m_DistanceFromWaterTableCanyon)          pixels = m_PixelsCanyon;
            else if (yNormalized >= m_DistanceFromWaterTableKeyhole)    pixels = m_PixelsKeyhole;
            else                                                        pixels = m_PixelsBed;
        }


        m_CellSize = discRadius / Mathf.Sqrt(2.0f);
        m_GridSize = new Vector2Int((int)(regionSize.x / m_CellSize), (int)(regionSize.y / m_CellSize));

        int[,] grid = new int[m_GridSize.y, m_GridSize.x];
        for (int y = 0; y < m_GridSize.y; y++)
        {
            for (int x = 0; x < m_GridSize.x; x++)
            {
                grid[y, x] = -1;
            }
        }

        List<Vector3> points = new List<Vector3>();
        List<Vector3> spawnPoints = new List<Vector3>();

        spawnPoints.Add(new Vector3(m_CellSize * m_GridSize.x / 2, m_CellSize * m_GridSize.y / 2, 0.0f));
        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector3 spawnCenter = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < m_SamplesBeforeRejection; i++)
            {
                float angle = Random.value * 2 * Mathf.PI;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
                Vector3 candidate = spawnCenter + dir * Random.Range(discRadius, 2.0f * discRadius);

                if (PositionIsValid(candidate, grid, points, discRadius) && FitsInImage(candidate))
                {
                    grid[(int)(candidate.y / m_CellSize), (int)(candidate.x / m_CellSize)] = points.Count;
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    candidateAccepted = true;
                    break;
                }
            }
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        Vector3 flatTangent = new Vector3(tangent.x, 0.0f, tangent.z).normalized;
        if (flatTangent == Vector3.zero) flatTangent = Vector3.forward;

        float flatTangentAngle = Mathf.Atan2(flatTangent.x, -flatTangent.z);
        float flatTangentCos = Mathf.Cos(flatTangentAngle);
        float flatTangentSin = Mathf.Sin(flatTangentAngle);
        Vector3 normal = Vector3.Cross(flatTangent, tangent);
        float upTangentAngle = Mathf.Acos(Vector3.Dot(tangent, flatTangent)) / Mathf.PI * 180.0f;
        //Debug.Log(flatTangentAngle + ", " + upTangentAngle + ", cross:" + normal + " = " + tangent + " x " + flatTangent);
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = new Vector3(points[i].x - regionSize.x / 2.0f , points[i].y - regionSize.y / 2.0f, 0.0f);
            points[i] = new Vector3(points[i].x * flatTangentCos, points[i].y, points[i].x * flatTangentSin);
            points[i] = Quaternion.AngleAxis(upTangentAngle, normal) * points[i];
        }

        /*foreach (var pos in points)
        {
            Transform t = Instantiate(m_Prefab);
            t.localPosition = pos;
            t.localScale = Vector3.one * m_Radius;
        }*/
        return points;
    }

    // Returns true if the candidate is positioned inside the area and does not collide with any existing discs.
    private bool PositionIsValid(Vector3 candidate, int[,] grid, List<Vector3> points, float discRadius)
    {
        if (candidate.x < 0.0f || m_CellSize * m_GridSize.x <= candidate.x || candidate.y < 0.0f || m_CellSize * m_GridSize.y <= candidate.y) return false;
        
        int cellX = (int) (candidate.x / m_CellSize);
        int cellY = (int) (candidate.y / m_CellSize);
        int y0 = Mathf.Max(cellY - 2, 0);
        int yEnd = Mathf.Min(cellY + 2, m_GridSize.y - 1);
        int x0 = Mathf.Max(cellX - 2, 0);
        int xEnd = Mathf.Min(cellX + 2, m_GridSize.x - 1);

        for (int y = y0; y <= yEnd; y++)
        {
            for (int x = x0; x <= xEnd; x++)
            {
                int index = grid[y, x];
                if (index != -1)
                {
                    float sqrDist = (candidate - points[index]).sqrMagnitude;
                    if (sqrDist < discRadius * discRadius) return false;
                }
            }
        }
        return true;
    }

    // Returns true if the point is inside the black part of the selected cave image.
    private bool FitsInImage(Vector2 candidate)
    {
        int x = (int)(m_ImageBed.width * candidate.x / (m_CellSize * m_GridSize.x));
        int y = (int)(m_ImageBed.height * candidate.y / (m_CellSize * m_GridSize.y));

        if (pixels[y * m_ImageBed.width + x] == Color.white)
        {
            return false;
        }
        return true;
    }
}
