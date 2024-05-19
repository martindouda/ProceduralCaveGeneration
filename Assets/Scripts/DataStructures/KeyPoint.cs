/*
 * Project: Procedural Generation of Cave Systems
 * File: KeyPoints.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file defines a class for representing key points. These are the points the paths have
 * to pass through.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class used to represent a key point.
public class KeyPoint : MonoBehaviour
{
    private Vector3 m_LastPos = Vector3.zero;
    public Vector3 LastPos { get => m_LastPos; set => m_LastPos = value; }
}
