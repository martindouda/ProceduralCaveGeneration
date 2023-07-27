using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class HeapTest : MonoBehaviour
{
    public int AddedItem = 0;

    public Heap<Node> Heap = new Heap<Node>(999);

    public class Node : IHeapItem<Node>
    {
        private int m_HeapIndex;
        public int Value;

        public Node(int value)
        {
            Value = value;
        }

        public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }

        public int CompareTo(object obj)
        {
            Node other = obj as Node;

            return other.Value.CompareTo(Value);
        }
    }
}
