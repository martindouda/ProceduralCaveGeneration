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
    [SerializeField] private Texture2D m_Image;
    [SerializeField] private Transform m_Prefab;

    [SerializeField] private float m_Radius = 0.5f;
    [SerializeField] private int m_SamplesBeforeRejection = 30;
    [SerializeField] private Vector2 m_RegionSize = new Vector2(10.0f, 10.0f);
    [SerializeField] Vector3 tangent = new Vector3(1.0f, 1.0f, -1.0f);

    private Vector2Int m_GridSize;
    private float m_CellSize;
    private Color[] pixels;

    private void Awake()
    {
        pixels = m_Image.GetPixels();
    }

    void Start()
    {
        pixels = m_Image.GetPixels();
        Color getPixel(int y, int x) { return pixels[y * m_Image.width + x]; }

        var whitePixels = 0;
        var blackPixels = 0;
        for (int y = 0; y < m_Image.height; y++)
        {
            for (int x = 0; x < m_Image.width; x++)
            {
                Color pixel = getPixel(y, x);

                if (pixel == Color.white)
                    whitePixels++;
                else
                    blackPixels++;
            }
        }

        Debug.Log("whit: " + whitePixels + ", black: " + blackPixels);
        tangent = tangent.normalized;
        GeneratePoints(tangent);
        transform.LookAt(transform.position - tangent);
    }
    
    public List<Vector3> GeneratePoints(Vector3 tangent)
    {
        pixels = m_Image.GetPixels();

        m_CellSize = m_Radius / Mathf.Sqrt(2.0f);
        m_GridSize = new Vector2Int((int)(m_RegionSize.x / m_CellSize), (int)(m_RegionSize.y / m_CellSize));

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
                Vector3 candidate = spawnCenter + dir * Random.Range(m_Radius, 2.0f * m_Radius);

                if (PositionIsValid(candidate, grid, points) && FitsInImage(candidate))
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
            points[i] = new Vector3(points[i].x - m_RegionSize.x / 2.0f , points[i].y - m_RegionSize.y / 2.0f, 0.0f);
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

    private bool PositionIsValid(Vector3 candidate, int[,] grid, List<Vector3> points)
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
                    if (sqrDist < m_Radius * m_Radius) return false;
                }
            }
        }
        return true;
    }

    private bool FitsInImage(Vector2 candidate)
    {
        int x = (int)(m_Image.width * candidate.x / (m_CellSize * m_GridSize.x));
        int y = (int)(m_Image.height * candidate.y / (m_CellSize * m_GridSize.y));

        if (pixels[y * m_Image.width + x] == Color.white)
        {
            return false;
        }
        return true;
    }
}
