/*
 * Project: Procedural Generation of Cave Systems
 * File: Fracture.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file defines a class for representing fractures, within the Unity environment. Fractures essentially define the direction
 * in which traveling is cheaper. The script includes functionality for specifying the normal vector inside Unity Editor.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class used to represent a fracture.
public class Fracture : MonoBehaviour
{
    public Vector3 NormalVector;
}
