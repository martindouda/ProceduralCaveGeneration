using UnityEngine;

public static class MarchingSquaresTables
{
    public static int[][] edgeConnections = {
        new int[] {0,1}, new int[] {1,2}, new int[] {2,3}, new int[] {3,0},
        new int[] {0,0}, new int[] {1,1}, new int[] {2,2}, new int[] {3,3},
    };

    public static Vector3[] squareCorners = new Vector3[] {
        new Vector3(1, 0, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 0, 1),
        new Vector3(0, 0, 0),
    };

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
