using System.Collections.Generic;
using UnityEngine;
using static PoissonSpheres;

// Class used to represent a path between two key points.
public class Path
{
    private List<Point> m_Points;
    private float m_Cost;

    public Point Start { get => m_Points[0]; }
    public Point End { get => m_Points[m_Points.Count - 1]; }
    public float Cost { get => m_Cost; }

    // Constructor which takes all the points on the path including the start
    // and the end and a cost of the path.
    public Path(List<Point> points, float cost)
    {
        m_Points = points;
        m_Cost = cost;
    }

    // Vizualizes the path using a line renderer.
    public void Visualize(PoissonSpheres poissonSpheres, LineRenderer lineRenderer)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var point in m_Points)
        {
            positions.Add(point.Pos - new Vector3(poissonSpheres.Size.x / 2, 0.0f, poissonSpheres.Size.y / 2));
            point.VisualSphereType = SphereType.GREEN;
        }
        lineRenderer.positionCount = m_Points.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }
}
