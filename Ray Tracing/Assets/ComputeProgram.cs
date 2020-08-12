using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComputeProgram : MonoBehaviour
{
    public static int SizeOfSphere = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere));
    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };

    public Image image;

    public ComputeShader compute;
    public ComputeShader sphereCreation;
    public Texture skyboxTexture;
    public Light directionalLight;

    public int reflectionCount = 4;

    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public int SphereCount = 100;
    public float SpherePlacementRadius = 100.0f;
    private ComputeBuffer sphereBuffer;

    private RenderTexture result;
    private new Camera camera;

    void Start()
    {
        camera = Camera.main;

        InitTexture();
        SetUpScene();
    }

    void Update()
    {
        var kernel = compute.FindKernel("CSMain");

        Vector3 l = directionalLight.transform.forward;
        compute.SetVector("DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));

        compute.SetInt("ReflectionCount", reflectionCount);
        compute.SetBuffer(kernel, "Spheres", sphereBuffer);
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

    public void SetUpScene()
    {
        var kernel = sphereCreation.FindKernel("CSMain");
        sphereBuffer = new ComputeBuffer(SphereCount, SizeOfSphere);

        sphereCreation.SetInt("stride", SizeOfSphere);
        sphereCreation.SetInt("SphereCount", SphereCount);
        sphereCreation.SetFloat("SpherePlacementRadius", SpherePlacementRadius);
        sphereCreation.SetBuffer(kernel, "Spheres", sphereBuffer);

        sphereCreation.Dispatch(kernel, Mathf.CeilToInt(SphereCount / 100f), 1, 1);
    }
    public void SetUpScene2()
    {
        List<Sphere> spheres = new List<Sphere>();
        // Add a number of random spheres
        while (spheres.Count < SphereCount)
        {
            Sphere sphere = new Sphere();
            // Radius and radius
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }
            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? Vector3.one * 0.9f : Vector3.one * 0.04f;
            // Add the sphere to the list
            spheres.Add(sphere);
        SkipSphere:
            continue;
        }
        // Assign to compute buffer
        sphereBuffer = new ComputeBuffer(spheres.Count, SizeOfSphere);
        sphereBuffer.SetData(spheres);

    }
}
