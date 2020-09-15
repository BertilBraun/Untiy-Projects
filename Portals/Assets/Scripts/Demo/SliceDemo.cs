using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceDemo : Traveller
{
    public float speed = 1f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            speed *= -1f;

        transform.position -= transform.forward * speed * Time.deltaTime;
    }
}
