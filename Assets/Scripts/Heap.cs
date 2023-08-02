using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public interface IHeapItem<T> : IComparable
{
    public int HeapIndex { get; set; }
}

public class Heap<T> where T : IHeapItem<T>
{
    private T[] m_Data;
    private int m_ItemCount;
    public Heap(int capacity)
    {
        m_Data = new T[capacity];
        m_ItemCount = 0;
    }

    public int Count { get => m_ItemCount; }

    public void Add(T item)
    {
        item.HeapIndex = m_ItemCount;
        m_Data[m_ItemCount] = item;
        m_ItemCount++;
        SortUp(item);
    }

    public T Pop()
    {
        T ret = m_Data[0];
        m_Data[0] = m_Data[--m_ItemCount];
        m_Data[0].HeapIndex = 0;
        SortDown(m_Data[0]);
        return ret;
    }

    public void Update(T item)
    {
        SortUp(item);
    }

    private void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) /2;
        T parentItem = m_Data[parentIndex];
        if (item.CompareTo(parentItem) > 0)
        {
            Swap(item, parentItem);
            SortUp(item);
        }
    }

    private void SortDown(T item)
    {
        int leftIndex = item.HeapIndex * 2 + 1;
        int rightIndex = item.HeapIndex * 2 + 2;



        if (leftIndex >= m_ItemCount) return;

        int swapIndex = leftIndex;

        if (rightIndex < m_ItemCount)
        {
            if (m_Data[rightIndex].CompareTo(m_Data[leftIndex]) > 0)
            {
                swapIndex = rightIndex;
            }
        }

        if (item.CompareTo(m_Data[swapIndex]) > 0) return;

        Swap(item, m_Data[swapIndex]);
        SortDown(item);
    }

    private void Swap(T i1, T i2)
    {
        m_Data[i1.HeapIndex] = i2;
        m_Data[i2.HeapIndex] = i1; 
        int temp = i1.HeapIndex;
        i1.HeapIndex = i2.HeapIndex;
        i2.HeapIndex = temp;
    }
}
