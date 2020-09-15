using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    private Portal[] portals;

    void Awake()
    {
        portals = FindObjectsOfType<Portal>();
    }

    void OnPreCull()
    {
        foreach (var portal in portals)
            portal.PrePortalRender();

        foreach (var portal in portals)
            portal.Render();

        foreach (var portal in portals)
            portal.PostPortalRender();
    }
}
