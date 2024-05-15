using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

public class VoronoiDiagram : MonoBehaviour
{
    [Range(0, 1)]
    public float Randomness = 1;
    public int gridSize = 10;
    private int imgSize;
    private RawImage image;
    private int pixelsPerCell;
    private Vector2[,] pointsPositions;
    private Color[,] colors;
    private float randomness;
    private Texture2D texture;
    private float lastUpdateTime = -1;

    void Start()
    {
        Profiler.BeginSample("VoronoiDiagramCPU");
        image = GetComponent<RawImage>();
        imgSize = Mathf.RoundToInt(image.GetComponent<RectTransform>().sizeDelta.x);
        GnerateDiagram();
        randomness = Randomness;
        lastUpdateTime = Time.time;
        Profiler.EndSample();
    }

    void Update()
    {
        if (Time.time - lastUpdateTime > 0.03 && randomness != Randomness)
        {
            lastUpdateTime = Time.time;
            SetColors();
        }
    }

    void GnerateDiagram()
    {
        texture = new Texture2D(imgSize, imgSize);
        texture.filterMode = FilterMode.Point;
        pixelsPerCell = imgSize / gridSize;
        GeneratePoints();

        image.texture = texture;

    }

    private void GeneratePoints()
    {
        pointsPositions = new Vector2[gridSize, gridSize];
        colors = new Color[gridSize, gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                pointsPositions[i, j] = new Vector2(i * pixelsPerCell + UnityEngine.Random.Range(0, pixelsPerCell),
                    j * pixelsPerCell + UnityEngine.Random.Range(0, pixelsPerCell));

                float r = UnityEngine.Random.Range(0, 1f);
                float g = UnityEngine.Random.Range(0, 1f);
                float b = UnityEngine.Random.Range(0, 1f);
                Color c = new Color(r, g, b, 1);
                colors[i, j] = c;
            }
        }
        SetColors();
    }

    private void SetColors()
    {
        for (int i = 0; i < imgSize; i++)
        {
            for (int j = 0; j < imgSize; j++)
            {
                int gridX = i / pixelsPerCell;
                int gridY = j / pixelsPerCell;

                float nearestDistance = Mathf.Infinity;
                Vector2Int nearestPoint = new Vector2Int();

                // 取九宫格周围的点
                for (int a = -1; a < 2; a++)
                {
                    for (int b = -1; b < 2; b++)
                    {
                        int X = gridX + a;
                        int Y = gridY + b;
                        if (X < 0 || Y < 0 || X >= gridSize || Y >= gridSize)
                        {
                            continue;
                        }

                        float distance = Vector2.Distance(new Vector2(i, j), new Vector2(
                            Mathf.Lerp(X * pixelsPerCell, pointsPositions[X, Y].x, Randomness),
                            Mathf.Lerp(Y * pixelsPerCell, pointsPositions[X, Y].y, Randomness)
                            ));
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestPoint = new Vector2Int(X, Y);
                        }
                    }
                }

                texture.SetPixel(i, j, colors[nearestPoint.x, nearestPoint.y]);
            }
        }
        texture.Apply();
    }
}