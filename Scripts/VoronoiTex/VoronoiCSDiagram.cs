using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class VoronoiCSDiagram : MonoBehaviour
{
    [Range(0, 1)]
    public float Randomness = 1;
    public int gridSize = 10;
    private int imgSize;
    private RawImage image;
    private int pixelsPerCell;
    private Vector4[] pointsPositions;
    private Vector4[] colors;
    private float randomness;
    private float lastUpdateTime = -1;
    private int voronoiMainKernel;
    RenderTexture texture;

    public ComputeShader computeShader;

    void Start()
    {
        Profiler.BeginSample("VoronoiDiagramGPU");
        image = GetComponent<RawImage>();
        imgSize = Mathf.RoundToInt(image.GetComponent<RectTransform>().sizeDelta.x);
        randomness = Randomness;
        GnerateDiagram();
        lastUpdateTime = Time.time;
        Profiler.EndSample();
    }

    void Update()
    {
        if (randomness != Randomness)
        {
            Profiler.BeginSample("VoronoiDiagramGPU");
            lastUpdateTime = Time.time;
            randomness = Randomness;
            //computeShader.SetTexture(voronoiMainKernel, "Result", texture);
            computeShader.SetFloat("randomness", randomness);
            computeShader.Dispatch(voronoiMainKernel, 128, 128, 1);
            Profiler.EndSample();
        }
    }

    void GnerateDiagram()
    {
        texture = new RenderTexture(1024, 1024, 16);
        texture.filterMode = FilterMode.Point;
        texture.enableRandomWrite = true;
        pixelsPerCell = imgSize / gridSize;
        texture.Create();
        GeneratePoints();

        voronoiMainKernel = computeShader.FindKernel("VoronoiMain");
        computeShader.SetVectorArray("pointsPositions", pointsPositions);
        computeShader.SetVectorArray("colors", colors);
        computeShader.SetFloat("pixelsPerCell", pixelsPerCell);
        computeShader.SetInt("gridSize", gridSize);
        computeShader.SetTexture(voronoiMainKernel, "Result", texture);
        computeShader.SetFloat("randomness", randomness);
        computeShader.Dispatch(voronoiMainKernel, 128, 128, 1);



        image.texture = texture;
    }

    private void GeneratePoints()
    {
        pointsPositions = new Vector4[gridSize * gridSize];
        colors = new Vector4[gridSize * gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                pointsPositions[i + j * gridSize] = new Vector4(i * pixelsPerCell + UnityEngine.Random.Range(0, pixelsPerCell),
                    j * pixelsPerCell + UnityEngine.Random.Range(0, pixelsPerCell), 0, 0);

                float r = UnityEngine.Random.Range(0, 1f);
                float g = UnityEngine.Random.Range(0, 1f);
                float b = UnityEngine.Random.Range(0, 1f);
                Color c = new Vector4(r, g, b, 1);
                colors[i + j * gridSize] = c;
            }
        }
    }
}