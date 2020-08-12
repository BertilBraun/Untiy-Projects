using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeProgram : MonoBehaviour
{
    public ComputeShader compute;
    public RenderTexture result;

    public Color color;

    void Start()
    {
        result.Release();
        result.enableRandomWrite = true;
        result.Create();
    }

    void Update()
    {
        var kernel = compute.FindKernel("CSMain");
        compute.SetTexture(kernel, "Result", result);
        compute.SetVector("Color", color);
        compute.Dispatch(kernel, result.width / 8, result.height / 8, 1);
    }
}
