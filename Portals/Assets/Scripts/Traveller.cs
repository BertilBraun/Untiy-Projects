using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Traveller : MonoBehaviour
{
    public Vector3 oldPos;
    public GameObject graphicsObject;
    public GameObject graphicsClone { get; set; }

    public Material[] originalMaterials { get; set; }
    public Material[] cloneMaterials { get; set; }

    public void OnEnter()
    {
        if (graphicsClone == null)
        {
            graphicsClone = Instantiate(graphicsObject);
            graphicsClone.transform.parent = graphicsObject.transform.parent;
            graphicsClone.transform.localScale = graphicsObject.transform.localScale;
            originalMaterials = GetMaterials(graphicsObject);
            cloneMaterials = GetMaterials(graphicsClone);
        }
        else
            graphicsClone.SetActive(true);
    }

    public void OnLeave()
    {
        graphicsClone.SetActive(false);
    }

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }

    public void SetSliceOffsetDst(float dst, bool clone)
    {
        for (int i = 0; i < originalMaterials.Length; i++)
            if (clone)
                cloneMaterials[i].SetFloat("sliceOffsetDst", dst);
            else
                originalMaterials[i].SetFloat("sliceOffsetDst", dst);
    }

    Material[] GetMaterials(GameObject g)
    {
        var renderers = g.GetComponentsInChildren<MeshRenderer>();
        var matList = new List<Material>();
        foreach (var renderer in renderers)
            foreach (var mat in renderer.materials)
                matList.Add(mat);
     
        return matList.ToArray();
    }
}
