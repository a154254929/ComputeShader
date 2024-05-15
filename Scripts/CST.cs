using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CST : MonoBehaviour
{
    private RenderTexture texture;
    public ComputeShader computeShader;
    public Material material;
    private int kernal;
    void Start()
    {
        texture = new RenderTexture(256, 256, 16);
        texture.enableRandomWrite = true;
        texture.Create();
        kernal = computeShader.FindKernel("CSMain");

        material.SetTexture("_MainTex", texture);
    }
    void Update()
    {
        SetupComputeShader();
    }

    private void SetupComputeShader()
    {
        computeShader.SetTexture(kernal, "Result", texture);
        computeShader.Dispatch(kernal, 32, 32, 1);
    }
}