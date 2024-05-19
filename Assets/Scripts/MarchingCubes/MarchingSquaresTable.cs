/*
 * Project: Procedural Generation of Cave Systems
 * File: MarchingSquaresTables.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: Provides the necessary data for the Marching Squares algorithm for generating 2D surfaces from scalar fields.
 * Each square in the contents grid is processed to determine its surface configuration, resulting in a mesh of triangles.
 */
using UnityEngine;

// This class contains tables used in Marching Squares algorithm for generating 2D contours.
public static class MarchingSquaresTables
{
    // Table containing edge connections for each square configuration
    public static int[][] edgeConnections = {
        new int[] {0,1}, new int[] {1,2}, new int[] {2,3}, new int[] {3,0},
        new int[] {0,0}, new int[] {1,1}, new int[] {2,2}, new int[] {3,3},
    };

    // Table containing the coordinates of the corners of the square
    public static Vector3[] squareCorners = new Vector3[] {
        new Vector3(1, 0, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 0, 1),
        new Vector3(0, 0, 0),
    };

    // Table containing the triangle configurations for each square configuration
    public static int[][] triTable = {
        new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int[] { 0,  3,  4, -1, -1, -1, -1, -1, -1, -1},
        new int[] { 1,  0,  5, -1, -1, -1, -1, -1, -1, -1},
        new int[] { 1,  3,  4,  1,  4,  5, -1, -1, -1, -1},
        new int[] { 2,  1,  6, -1, -1, -1, -1, -1, -1, -1},
        new int[] { 0,  3,  4,  2,  1,  6, -1, -1, -1, -1},
        new int[] { 2,  0,  5,  2,  5,  6, -1, -1, -1, -1},
        new int[] { 2,  3,  4,  2,  4,  6,  4,  5,  6, -1},
        new int[] { 3,  2,  7, -1, -1, -1, -1, -1, -1, -1},
        new int[] { 0,  2,  7,  0,  7,  4, -1, -1, -1, -1},
        new int[] { 1,  0,  5,  3,  2,  7, -1, -1, -1, -1},
        new int[] { 1,  2,  7,  1,  7,  5,  7,  4,  5, -1},
        new int[] { 3,  1,  7,  1,  6,  7, -1, -1, -1, -1},
        new int[] { 0,  1,  6,  0,  6,  4,  6,  7,  4, -1},
        new int[] { 3,  0,  5,  3,  5,  7,  5,  6,  7, -1},
        new int[] { 0,  1,  2,  0,  2,  3, -1, -1, -1, -1},
    };
}