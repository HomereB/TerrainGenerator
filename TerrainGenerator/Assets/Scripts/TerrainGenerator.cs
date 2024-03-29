﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{

    public int width, height, depth;
    public float scale;
    // Start is called before the first frame update
    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);
        float[,] map = GenerateHeights();
        terrainData.SetHeights(0, 0, map);

        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                heights[x, y] = CalculateHeight(x, y);
            }
        }

        return heights;
    }

    float CalculateHeight(int x,  int y)
    {
        if (scale <= 0)
            scale = 0.0001f;

        float xCoord = (float) x /  scale;
        float yCoord = (float) y /  scale;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }

    void Erode(float[,] map)
    {

    }
}
