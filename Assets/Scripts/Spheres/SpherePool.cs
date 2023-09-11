using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpherePool : MonoBehaviour
{
    [SerializeField] private GameObject m_SpherePrefab;
    [SerializeField] private Transform m_SpheresParent;
    [Space(40)]
    [SerializeField] private int m_InitialNumOfSpheres = 5000;


    private List<Transform> m_Spheres = new List<Transform>();
    private int m_NumSpheresInUse = 0;

    public void CleanUpSpheresOnSceneLoad()
    {
        for (int i = m_SpheresParent.childCount - 1; i >= 0; i--)
        {
            Transform child = m_SpheresParent.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
    }

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

    public void NewRound()
    {
        m_NumSpheresInUse = 0;
    }

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

    public void PutUnusedToSleep()
    {
        for (int i = m_NumSpheresInUse; i < m_Spheres.Count; i++)
        {
            m_Spheres[i].gameObject.SetActive(false);
        }
        m_NumSpheresInUse = 0;
    }

    public void DeleteSpheres()
    {
        foreach (var sphere in m_Spheres)
        {
            DestroyImmediate(sphere.gameObject);
        }
        m_Spheres.Clear();
    }

    public void OnDestroy()
    {
        DeleteSpheres();
    }
}
