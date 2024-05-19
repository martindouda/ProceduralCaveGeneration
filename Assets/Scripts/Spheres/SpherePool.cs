/*
 * Project: Procedural Generation of Cave Systems
 * File: SpherePool.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file manages the instantiation, management, and deletion of spheres within the Unity environment. It provides functionality
 * for maintaining a pool of spheres, allowing for efficient reuse and management during runtime. Additionally, it includes methods for cleaning
 * up spheres on scene load, increasing sphere capacity, waking up and putting unused spheres to sleep, and deleting spheres when necessary.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Class which takes care of the instantiation, management and deletion of the spheres.
public class SpherePool : MonoBehaviour
{
    [SerializeField] private GameObject m_SpherePrefab;
    [SerializeField] private Transform m_SpheresParent;
    [Space(40)]
    [SerializeField] private int m_InitialNumOfSpheres = 5000;


    private List<Transform> m_Spheres = new List<Transform>();
    private int m_NumSpheresInUse = 0;

    // Destroys all the spheres which remain from the previous session.
    public void CleanUpSpheresOnSceneLoad()
    {
        for (int i = m_SpheresParent.childCount - 1; i >= 0; i--)
        {
            Transform child = m_SpheresParent.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
        m_Spheres = new List<Transform>();
        IncreaseCapacity();
    }

    // Increases the number of available spheres.
    private void IncreaseCapacity()
    {
        int limit = m_Spheres.Count + m_InitialNumOfSpheres;
        for (int i = m_Spheres.Count; i < limit; i++)
        {
            GameObject go = Instantiate(m_SpherePrefab, m_SpheresParent);
            go.name = "Sphere " + i;
            m_Spheres.Add(go.transform);
        }
    }

    // Resets the number of spheres in use.
    public void NewRound()
    {
        m_NumSpheresInUse = 0;
    }

    // Wakes up a sphere.
    public void WakeSphere(Vector3 pos, float scale, Material mat, int index)
    {
        if (m_Spheres.Count < m_NumSpheresInUse + 1)
        {
            IncreaseCapacity();
        }
        Transform sphereTransform = m_Spheres[m_NumSpheresInUse++];
        sphereTransform.position = pos;
        sphereTransform.localScale = Vector3.one * scale;
        sphereTransform.gameObject.GetComponent<Renderer>().material = mat;
        sphereTransform.gameObject.SetActive(true);
    }

    // Turns all the unused spheres.
    public void PutUnusedToSleep()
    {
        for (int i = m_NumSpheresInUse; i < m_Spheres.Count; i++)
        {
            m_Spheres[i].gameObject.SetActive(false);
        }
        m_NumSpheresInUse = 0;
    }

    // Deletes all the referenced spheres.
    public void DeleteSpheres()
    {
        foreach (var sphere in m_Spheres)
        {
            DestroyImmediate(sphere.gameObject);
        }
        m_Spheres.Clear();
    }

    // Deletes all the referenced spheres on this object's deletion.
    public void OnDestroy()
    {
        DeleteSpheres();
    }
}
