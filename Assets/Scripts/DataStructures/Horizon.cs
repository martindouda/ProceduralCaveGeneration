using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Horizon : MonoBehaviour, IHeapItem<Horizon>
{
    private int m_HeapIndex;

    [SerializeField] private float m_Cost;

    public float Height { get => transform.position.y; }
    public float Cost { get => m_Cost; }

    public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }

    public int CompareTo(object obj)
    {
        Horizon other = obj as Horizon;
        return other.Height.CompareTo(Height);
    }
}