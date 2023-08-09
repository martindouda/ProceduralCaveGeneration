using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyPoint : MonoBehaviour
{
    private Vector3 m_LastPos = Vector3.zero;
    public Vector3 LastPos { get => m_LastPos; set => m_LastPos = value; }
}
