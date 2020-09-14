using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Traveller : MonoBehaviour
{
    public Vector3 pos;
    public Vector3 oldPos;


    void Update()
    {
        oldPos = pos;
        pos = transform.position;
    }

    public void Teleport(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }
}
