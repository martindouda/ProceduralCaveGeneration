/*
 * Project: Procedural Generation of Cave Systems
 * File: PoissonSpheres.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file provides functionality for generating and managing Poisson's spheres within the Unity environment. Poisson's
 * spheres represent a random distribution of non-intersecting spheres in 3D space. The script also includes methods for
 * calculating the nearest point to a given position, as well as grid position retrieval.
*/

using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

// Class which generates and manages Poisson's spheres.
// Poisson's spheres is a random sphere distribution of non intersecting spheres in the 3D space.
public class PoissonSpheres
{
    private Vector3Int m_Size;
    private float m_MinSphereRadius;
    private float m_MaxSphereRadius;
    private float m_CellSize;
    private float m_SpacingLimit;

    private int[,,] m_Grid;
    private int m_GridSizeX, m_GridSizeY, m_GridSizeZ;

    private List<Point> m_Points = new List<Point>();

    public List<Point> Points { get => m_Points; }

    public Vector3 Size { get => m_Size; }

    public int GridSizeX { get => m_GridSizeX; }
    public int GridSizeY { get => m_GridSizeY; }
    public int GridSizeZ { get => m_GridSizeZ; }

    public int[,,] Grid { get => m_Grid; set => m_Grid = value; }

    // Enum is used to determin what material a sphere should use.
    public enum SphereType
    {
        WHITE, BLUE, RED, GREEN, _SIZE
    }

    // Class which is used to represent a sphere.
    public class Point
    {
        public Vector3 Pos;
        public float Radius;
        public int Index;
        public List<NearestPoint> NextList;

        public SphereType VisualSphereType;

        public Point(Vector3 pos, float radius, int index)
        {
            Pos = pos;
            Radius = radius;
            NextList = new List<NearestPoint>();
            VisualSphereType = SphereType.WHITE;
            Index = index;
        }
     }

    // Class which is used to represent spheres neighbouring with a sphere.
    public class NearestPoint : IHeapItem<NearestPoint>
    {

        private int m_HeapIndex;
        private int m_PointIndex;
        private float m_SqrDist;
        private float m_Dist;

        public NearestPoint(int pointIndex, float sqrDist)
        {
            m_PointIndex = pointIndex;
            m_SqrDist = sqrDist;
        }

        public void CalculateDist()
        {
            m_Dist = Mathf.Sqrt(m_SqrDist);
        }

        public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }
        public int PointIndex { get => m_PointIndex; }
        public float Dist { get => m_Dist; }

        public int CompareTo(object obj)
        {
            NearestPoint other = obj as NearestPoint;
            return other.m_SqrDist.CompareTo(m_SqrDist);
        }
    }

    // Constructor which initializes the variables.
    public PoissonSpheres(Vector3Int size, float minSphereRadius, float maxSphereRadius, float spacingLimit) 
    {
        m_Size = size;
        m_MinSphereRadius = minSphereRadius;
        m_MaxSphereRadius = maxSphereRadius;
        m_CellSize = 2 * minSphereRadius / Mathf.Sqrt(3);
        m_SpacingLimit = spacingLimit;

        m_GridSizeX = Mathf.CeilToInt(size.x / m_CellSize);
        m_GridSizeY = Mathf.CeilToInt(size.y / m_CellSize);
        m_GridSizeZ = Mathf.CeilToInt(size.z / m_CellSize);
    }

    // Generates the spheres.
    public void GeneratePoints(int numSamplesBeforeRejection)
    {
        m_Points = new List<Point>();
        List<Point> spawnPoints = new List<Point>();
        m_Grid = new int[m_GridSizeX, m_GridSizeY, m_GridSizeZ];
        for (int z = 0; z < m_GridSizeZ; z++)
        {
            for (int y = 0; y < m_GridSizeY; y++)
            {
                for (int x = 0; x < m_GridSizeX; x++)
                {
                    m_Grid[x, y, z] = -1;
                }
            }
        }


        spawnPoints.Add(new Point((Vector3)m_Size / 2.0f, Random.Range(m_MinSphereRadius, m_MaxSphereRadius), 0));
        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Point spawnerPoint = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float candidateRadius = Random.Range(m_MinSphereRadius, m_MaxSphereRadius);
                float minDistFromSpawn = spawnerPoint.Radius + candidateRadius;
                float distFromSpawn = Random.Range(minDistFromSpawn, m_SpacingLimit * minDistFromSpawn);
                Vector3 candidatePos = spawnerPoint.Pos + distFromSpawn * Random.onUnitSphere;
                Point candidatePoint = new Point(candidatePos, candidateRadius, m_Points.Count);

                if (!pointIsValid(candidatePoint)) continue;

                m_Grid[(int)(candidatePos.x / m_CellSize), (int)(candidatePos.y / m_CellSize), (int)(candidatePos.z / m_CellSize)] = candidatePoint.Index;
                m_Points.Add(candidatePoint);
                spawnPoints.Add(candidatePoint);
                candidateAccepted = true;
                break;
            }
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }
    }

    // Returns true if a sphere (Point) does not interesect with another sphere.
    private bool pointIsValid(Point point)
    {
        if (point.Pos.x < 0.0f || point.Pos.x > m_Size.x || point.Pos.y < 0.0f || point.Pos.y > m_Size.y || point.Pos.z < 0.0f || point.Pos.z > m_Size.z) return false;

        float floatGridPosX = point.Pos.x / m_CellSize;
        float floatGridPosY = point.Pos.y / m_CellSize;
        float floatGridPosZ = point.Pos.z / m_CellSize;

        float spawnRadiusSum = (point.Radius + m_MaxSphereRadius) / m_CellSize;
        int startX = Mathf.Max((int)(floatGridPosX - spawnRadiusSum), 0);
        int endX = Mathf.Min((int)(floatGridPosX + spawnRadiusSum), m_GridSizeX - 1);
        int startY = Mathf.Max((int)(floatGridPosY - spawnRadiusSum), 0);
        int endY = Mathf.Min((int)(floatGridPosY + spawnRadiusSum), m_GridSizeY - 1);
        int startZ = Mathf.Max((int)(floatGridPosZ - spawnRadiusSum), 0);
        int endZ = Mathf.Min((int)(floatGridPosZ + spawnRadiusSum), m_GridSizeZ - 1);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = m_Grid[x, y, z];
                    if (index == -1) continue;

                    float sqrDist = (point.Pos - m_Points[index].Pos).sqrMagnitude;
                    float radiusSum = point.Radius + m_Points[index].Radius;
                    if (sqrDist < radiusSum * radiusSum) return false;
                }
            }
        }

        return true;
    }   

    // Returns the index of the sphere closest to the position.
    public Point GetNearestPoint(Vector3 pos, int searchDistance)
    {
        Vector3 toCenterOffset = new Vector3(m_Size.x / 2.0f, 0.0f, m_Size.z / 2.0f);

        pos.x = Mathf.Clamp(pos.x, -toCenterOffset.x, toCenterOffset.x);
        pos.y = Mathf.Clamp(pos.y, 0, m_Size.y);
        pos.z = Mathf.Clamp(pos.z, -toCenterOffset.z, toCenterOffset.z);

        int startX = Mathf.Max((int)((pos.x + toCenterOffset.x) / m_CellSize) - searchDistance, 0);
        int endX = Mathf.Min((int)((pos.x + toCenterOffset.x) / m_CellSize) + searchDistance, m_GridSizeX - 1);
        int startY = Mathf.Max((int)(pos.y / m_CellSize) - searchDistance, 0);
        int endY = Mathf.Min((int)(pos.y / m_CellSize) + searchDistance, m_GridSizeY - 1);
        int startZ = Mathf.Max((int)((pos.z + toCenterOffset.z) / m_CellSize) - searchDistance, 0);
        int endZ = Mathf.Min((int)((pos.z + toCenterOffset.z) / m_CellSize) + searchDistance, m_GridSizeZ - 1);

        int searchWidth = (int)(2.0f * searchDistance) + 1;
        Heap<NearestPoint> heap = new Heap<NearestPoint>(searchWidth * searchWidth * searchWidth);


        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = m_Grid[x, y, z];
                    if (index == -1) continue;

                    heap.Add(new NearestPoint(index, (m_Points[index].Pos - toCenterOffset - pos).sqrMagnitude));
                }
            }
        }

        return m_Points[heap.Pop().PointIndex];
    }

    // Returns the position in the grid.
    public Vector3Int GetGridPos(Vector3 pos) 
    {
        return new Vector3Int((int)(pos.x / m_CellSize), (int)(pos.y / m_CellSize), (int)(pos.z / m_CellSize));
    }
}

