using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fracture : MonoBehaviour
{
    public Vector3 NormalVector;

    // Start is called before the first frame update
    void Start()
    {
        NormalVector = transform.up;
    }

    // Update is called once per frame
    void Update()
    {
        NormalVector = transform.up;
    }
}
