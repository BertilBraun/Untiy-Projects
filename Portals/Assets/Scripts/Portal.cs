using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Main Settings")]
    public Portal other;
    public MeshRenderer screen;

    [Header("Advanced Settings")]
    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

    Camera portalCam;
    Camera playerCam;

    RenderTexture renderTexture = null;
    List<Traveller> travellers = new List<Traveller>();

    void Awake()
    {
        portalCam = GetComponentInChildren<Camera>();
        portalCam.enabled = false;
        playerCam = Camera.main;
    }

    void LateUpdate()
    {
        UpdateTravellers();
    }

    void UpdateTravellers()
    {
        for (int i = 0; i < travellers.Count; i++)
        {
            var t = travellers[i];

            if (!SameSideOfPortal(t.pos, t.oldPos))
            {
                t.Teleport(playerCam.transform.position + (other.transform.position - transform.position), playerCam.transform.rotation);

                travellers.RemoveAt(i);
                i--;
            }
        }
    }

    public void Render()
    {
        //if (!CameraUtility.VisibleFromCamera(other.screen, playerCam))
        //    return;

        CreateRenderTexture();

        other.screen.enabled = false;

        other.portalCam.transform.position = other.transform.localToWorldMatrix * transform.worldToLocalMatrix * playerCam.transform.position;
        other.portalCam.transform.position = playerCam.transform.position + (other.transform.position - transform.position);
        other.portalCam.transform.rotation = playerCam.transform.rotation;

        
        //screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        SetNearClipPlane();
        other.portalCam.Render();


        //screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        other.screen.enabled = true;
        ProtectScreenFromClipping(playerCam.transform.position);
    }

    void CreateRenderTexture()
    {
        if (renderTexture != null && renderTexture.width == Screen.width && renderTexture.height == Screen.height)
            return;

        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        renderTexture.Create();

        screen.material.mainTexture = renderTexture;
        other.portalCam.targetTexture = renderTexture;
    }

    float ProtectScreenFromClipping(Vector3 viewPoint)
    {
        float halfHeight = playerCam.nearClipPlane * Mathf.Tan(playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCam.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, playerCam.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner;

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;
        screenT.localScale = new Vector3(screenT.localScale.x, screenT.localScale.y, screenThickness);
        screenT.localPosition = Vector3.forward * screenThickness * (camFacingSameDirAsPortal ? 0.5f : -0.5f);
        return screenThickness;
    }

    void SetNearClipPlane()
    {
        // Learning resource:
        // http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        Transform clipPlane = transform;
        int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - portalCam.transform.position));

        Vector3 camSpacePos = portalCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = portalCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + nearClipOffset;

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (Mathf.Abs(camSpaceDst) > nearClipLimit)
        {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // Update projection based on new clip plane
            // Calculate matrix with player cam so that player camera settings (fov, etc) are used
            portalCam.projectionMatrix = playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
        else
        {
            portalCam.projectionMatrix = playerCam.projectionMatrix;
        }
    }

    #region Triggers

    void OnTriggerEnter(Collider other)
    {
        var t = other.GetComponent<Traveller>();
        if (t)
            travellers.Add(t);
    }

    void OnTriggerExit(Collider other)
    {
        var t = other.GetComponent<Traveller>();
        if (t && travellers.Contains(t))
            travellers.Remove(t);
    }

    #endregion

    #region Helpers

    int SideOfPortal(Vector3 pos)
    {
        return System.Math.Sign(Vector3.Dot(pos - transform.position, transform.forward));
    }

    bool SameSideOfPortal(Vector3 posA, Vector3 posB)
    {
        return SideOfPortal(posA) == SideOfPortal(posB);
    }

    #endregion
}
