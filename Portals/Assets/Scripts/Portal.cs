using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Main Settings")]
    public Portal other;
    public MeshRenderer screen;
    public int recursionLimit = 5;

    [Header("Advanced Settings")]
    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

    Camera portalCam;
    Camera playerCam;

    RenderTexture renderTexture = null;
    MeshFilter screenMeshFilter;
    List<Traveller> travellers = new List<Traveller>();

    void Awake()
    {
        portalCam = GetComponentInChildren<Camera>();
        portalCam.enabled = false;
        playerCam = Camera.main;

        screenMeshFilter = screen.GetComponent<MeshFilter>();
        screen.material.SetInt("displayMask", 1);
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
            var tt = t.transform;

            var m = other.transform.localToWorldMatrix * transform.worldToLocalMatrix * tt.localToWorldMatrix;

            if (!SameSideOfPortal(tt.position, t.oldPos))
            {
                var positionOld = tt.position;
                var rotOld = tt.rotation;
                t.Teleport(transform, other.transform, m.GetColumn(3), m.rotation);
                t.graphicsClone.transform.SetPositionAndRotation(positionOld, rotOld);

                other.OnTravellerEnter(t);
                travellers.RemoveAt(i);
                i--;
            }
            else
            {
                t.graphicsClone.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                // Commented out in real Code UpdateSliceParams (t);
                t.oldPos = tt.position;
            }
        }
    }

    public void PrePortalRender()
    {
        foreach (var traveller in travellers)
            UpdateSliceParams(traveller);
    }

    public void PostPortalRender()
    {
        foreach (var traveller in travellers)
            UpdateSliceParams(traveller);
        ProtectScreenFromClipping(playerCam.transform.position);
    }

    public void Render()
    {
        if (!CameraUtility.VisibleFromCamera(other.screen, playerCam))
            return;

        CreateRenderTexture();

        var localToWorldMatrix = playerCam.transform.localToWorldMatrix;
        var renderPositions = new Vector3[recursionLimit];
        var renderRotations = new Quaternion[recursionLimit];

        int startIndex = 0;
        portalCam.projectionMatrix = playerCam.projectionMatrix;

        for (int i = 0; i < recursionLimit; i++)
        {
            if (i > 0 && !CameraUtility.BoundsOverlap(screenMeshFilter, other.screenMeshFilter, portalCam))
                break;

            localToWorldMatrix = transform.localToWorldMatrix * other.transform.worldToLocalMatrix * localToWorldMatrix;
            int renderOrderIndex = recursionLimit - i - 1;
            renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn(3);
            renderRotations[renderOrderIndex] = localToWorldMatrix.rotation;

            portalCam.transform.SetPositionAndRotation(renderPositions[renderOrderIndex], renderRotations[renderOrderIndex]);
            startIndex = renderOrderIndex;
        }

        // Hides Screen from rendering pass
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        other.screen.material.SetInt("displayMask", 0);

        for (int i = startIndex; i < recursionLimit; i++)
        {
            portalCam.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);
            SetNearClipPlane();
            HandleClipping();

            portalCam.Render();

            if (i == startIndex)
                other.screen.material.SetInt("displayMask", 1);
        }

        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    void HandleClipping()
    {
        // There are two main graphical issues when slicing travellers
        // 1. Tiny sliver of mesh drawn on backside of portal
        //    Ideally the oblique clip plane would sort this out, but even with 0 offset, tiny sliver still visible
        // 2. Tiny seam between the sliced mesh, and the rest of the model drawn onto the portal screen
        // This function tries to address these issues by modifying the slice parameters when rendering the view from the portal
        // Would be great if this could be fixed more elegantly, but this is the best I can figure out for now
        const float hideDst = -1000;
        const float showDst = 1000;
        float screenThickness = other.ProtectScreenFromClipping(portalCam.transform.position);

        foreach (var traveller in travellers)
        {
            if (SameSideOfPortal(traveller.transform.position, portalCam.transform.position))
                // Addresses issue 1
                traveller.SetSliceOffsetDst(hideDst, false);
            else
                // Addresses issue 2
                traveller.SetSliceOffsetDst(showDst, false);

            // Ensure clone is properly sliced, in case it's visible through this portal:
            int cloneSideOfLinkedPortal = -SideOfPortal(traveller.transform.position);
            bool camSameSideAsClone = other.SideOfPortal(portalCam.transform.position) == cloneSideOfLinkedPortal;
            if (camSameSideAsClone)
                traveller.SetSliceOffsetDst(screenThickness, true);
            else
                traveller.SetSliceOffsetDst(-screenThickness, true);
        }

        var offsetFromPortalToCam = portalCam.transform.position - transform.position;
        foreach (var linkedTraveller in other.travellers)
        {
            var travellerPos = linkedTraveller.transform.position;
            var clonePos = linkedTraveller.graphicsClone.transform.position;
            // Handle clone of linked portal coming through this portal:
            bool cloneOnSameSideAsCam = other.SideOfPortal(travellerPos) != SideOfPortal(portalCam.transform.position);
            if (cloneOnSameSideAsCam)
                // Addresses issue 1
                linkedTraveller.SetSliceOffsetDst(hideDst, true);
            else
                // Addresses issue 2
                linkedTraveller.SetSliceOffsetDst(showDst, true);

            // Ensure traveller of linked portal is properly sliced, in case it's visible through this portal:
            bool camSameSideAsTraveller = other.SameSideOfPortal(linkedTraveller.transform.position, portalCam.transform.position);
            if (camSameSideAsTraveller)
                linkedTraveller.SetSliceOffsetDst(screenThickness, false);
            else
                linkedTraveller.SetSliceOffsetDst(-screenThickness, false);
        }
    }

    void UpdateSliceParams(Traveller traveller)
    {
        // Calculate slice normal
        int side = SideOfPortal(traveller.transform.position);
        Vector3 sliceNormal = transform.forward * -side;
        Vector3 cloneSliceNormal = other.transform.forward * side;

        // Calculate slice centre
        Vector3 slicePos = transform.position;
        Vector3 cloneSlicePos = other.transform.position;

        // Adjust slice offset so that when player standing on other side of portal to the object, the slice doesn't clip through
        float sliceOffsetDst = 0;
        float cloneSliceOffsetDst = 0;
        float screenThickness = screen.transform.localScale.z;

        bool playerSameSideAsTraveller = SameSideOfPortal(playerCam.transform.position, traveller.transform.position);
        if (!playerSameSideAsTraveller)
        {
            sliceOffsetDst = -screenThickness;
        }
        bool playerSameSideAsCloneAppearing = side != other.SideOfPortal(playerCam.transform.position);
        if (!playerSameSideAsCloneAppearing)
        {
            cloneSliceOffsetDst = -screenThickness;
        }

        // Apply parameters
        for (int i = 0; i < traveller.originalMaterials.Length; i++)
        {
            traveller.originalMaterials[i].SetVector("sliceCentre", slicePos);
            traveller.originalMaterials[i].SetVector("sliceNormal", sliceNormal);
            traveller.originalMaterials[i].SetFloat("sliceOffsetDst", sliceOffsetDst);

            traveller.cloneMaterials[i].SetVector("sliceCentre", cloneSlicePos);
            traveller.cloneMaterials[i].SetVector("sliceNormal", cloneSliceNormal);
            traveller.cloneMaterials[i].SetFloat("sliceOffsetDst", cloneSliceOffsetDst);

        }

    }

    void CreateRenderTexture()
    {
        if (renderTexture != null && renderTexture.width == Screen.width && renderTexture.height == Screen.height)
            return;

        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        renderTexture.Create();

        other.screen.material.mainTexture = renderTexture;
        portalCam.targetTexture = renderTexture;
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

    void OnTravellerEnter(Traveller traveller)
    {
        if (!travellers.Contains(traveller))
        {
            traveller.oldPos = traveller.transform.position;
            traveller.OnEnter();
            travellers.Add(traveller);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var t = other.GetComponent<Traveller>();
        if (t)
            OnTravellerEnter(t);
    }

    void OnTriggerExit(Collider other)
    {
        var t = other.GetComponent<Traveller>();
        if (t && travellers.Contains(t))
        {
            t.OnLeave();
            travellers.Remove(t);
        }
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
