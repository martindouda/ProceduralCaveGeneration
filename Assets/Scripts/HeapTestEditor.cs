using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(HeapTest))]

public class HeapTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HeapTest heapTest = target as HeapTest;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add"))
        {
            heapTest.Heap.Add(new HeapTest.Node(heapTest.AddedItem));
            heapTest.Heap.Print();
        }
        heapTest.AddedItem = EditorGUILayout.IntField(heapTest.AddedItem);
        GUILayout.EndHorizontal();

    }
}
