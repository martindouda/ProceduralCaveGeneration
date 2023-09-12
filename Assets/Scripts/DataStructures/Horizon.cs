using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class used to represent a horizon.
public class Horizon : MonoBehaviour, IHeapItem<Horizon>
{
    private int m_HeapIndex;

    [SerializeField][Range(0.0f, 1.0f)] private float m_Cost;

    public float Height { get => transform.position.y; }
    public float Cost { get => m_Cost; }

    public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }

    // Function used to sort inside a heap.
    public int CompareTo(object obj)
    {
        Horizon other = obj as Horizon;
        return other.Height.CompareTo(Height);
    }
}