using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComputeProgram : MonoBehaviour
{
    public Image image;

    public ComputeShader compute;
    public Texture skyboxTexture;
    public Light directionalLight;

    public int reflectionCount = 4;

    private RenderTexture result;
    private new Camera camera;

    void Start()
    {
        camera = Camera.main;

        InitTexture();
    }

    void Update()
    {
        var kernel = compute.FindKernel("CSMain");

        Vector3 l = directionalLight.transform.position;
        compute.SetVector("Light", new Vector4(l.x, l.y, l.z, directionalLight.intensity));

        compute.SetFloat("iTime", Time.time);
        compute.SetInt("ReflectionCount", reflectionCount);
        compute.SetTexture(kernel, "SkyboxTexture", skyboxTexture);
        compute.SetTexture(kernel, "Result", result);
        compute.SetMatrix("CameraToWorld", camera.cameraToWorldMatrix);
        compute.SetMatrix("CameraInverseProjection", camera.projectionMatrix.inverse);

        int threadGroupsX = Mathf.CeilToInt(result.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(result.height / 8.0f);
        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
    }

    void InitTexture()
    {
        result = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        result.enableRandomWrite = true;
        result.Create();
        image.material.mainTexture = result;
    }
}
