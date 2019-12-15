﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    [Space(15)]

    [Header("Generation du terrain")]

    [Space(10)]
    public int seed;
    [Space(10)]
    public int xSize =256;
    public int zSize = 256;
    public int maxHeight;
    public int minHeight;

    [Space(10)]

    public float mainOffsetX;
    public float mainOffsetZ;

    public float secondaryOffsetX;
    public float secondaryOffsetZ;

    [Space(10)]
    public float mainScale = 1;
    public float secondaryScale = 1;

    public float mainNoiseMultiplicator;
    public float secondaryNoiseMultiplicator;

    [Space(10)]
    public TerrainType[] regions;

    [Space(15)]
    [Header("Generation des rivières")]

    [Space(10)]
    public int nbSources;
    public List<Vector2Int> sourceCoordinates;
    public float downhillHeightTolerance;
    public List<List<int>> flowGraphs;

    [Space(10)]
    public float uphillHeightTolerance;
    public List<float> riverDepth;
    public List<int> riverWidth;

    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Renderer renderr;
    Texture2D texture;

    System.Random rng;

    void Start()
    {
        rng = new System.Random(seed);
        mainOffsetX = (float)rng.Next(0, 10000);
        mainOffsetZ = (float)rng.Next(0, 10000);
        secondaryOffsetX = (float)rng.Next(0, 10000);
        secondaryOffsetZ = (float)rng.Next(0, 10000);
        mesh = new Mesh();

        GetComponent<MeshFilter>().mesh = mesh;
        renderr = GetComponent<Renderer>();

        renderr.material.mainTexture = GenerateTexture();
        CreateShape();

        //sourceCoordinates.Add(new Vector2Int(24, 24));
        //sourceCoordinates.Add(new Vector2Int(58, 80));
        //renderr.sharedMaterial.mainTexture = GenerateColorTexture();
        flowGraphs = new List<List<int>>(sourceCoordinates.Count);

        for(int i = 0; i<sourceCoordinates.Count;i++)
        {
            //vertices[sourceCoordinates[i].x + sourceCoordinates[i].y * (xSize + 1)].y *= 2;
            flowGraphs.Add(new List<int>());
            GenerateFlowingGraph(i);
        }

        riverDepth.Add(5);
        riverWidth.Add(5);
        riverDepth.Add(5);
        riverWidth.Add(5);

        for (int i =0; i< flowGraphs.Count;i++)
        {
            GenerateRiverDepth(i);
        }

        UpdateMesh();
        GetComponent<MeshFilter>().mesh.RecalculateBounds();
    }

    private void GenerateRiverDepth(int flowGraphIndex)
    {
        Dictionary<int,int> nearbynodes = new Dictionary<int, int>();
        int MaxValue = 0;
        int MaxIndex = -1;
        foreach (int index in flowGraphs[flowGraphIndex])
        { 
            for(int i = -riverWidth[flowGraphIndex] ;i<= riverWidth[flowGraphIndex];i++)
            {
                for (int j = -riverWidth[flowGraphIndex]; j <= riverWidth[flowGraphIndex]; j++)
                {
                    if(!nearbynodes.ContainsKey(index+j + i*(xSize+1)))
                    {
                        nearbynodes.Add(index + j + i * (xSize + 1), 1);
                    }
                    else
                    {
                        nearbynodes[index + j + i * (xSize + 1)] +=1;
                    }
                    if(nearbynodes[index + j + i * (xSize + 1)]>MaxValue)
                    {
                        MaxIndex = index + j + i * (xSize + 1);
                        MaxValue = nearbynodes[index + j + i * (xSize + 1)];
                    }
                }
            }
        }
        foreach (int index in nearbynodes.Keys)
        {
            vertices[index].y -= riverDepth[flowGraphIndex] * nearbynodes[index] / MaxValue;
        }

        //for(int i = -riverWidth[flowGraphIndex] ;i<= riverWidth[flowGraphIndex];i++)
        //{
        //    vertices[index + i].y -= riverDepth[flowGraphIndex]*Mathf.Sin(((float)Mathf.Abs(i)) / (float)riverWidth[flowGraphIndex]);
        //}

        //for (int i = -riverWidth[flowGraphIndex]; i <= riverWidth[flowGraphIndex]; i++)
        //{
        //    vertices[index + i*(xSize+1)].y -= riverDepth[flowGraphIndex]* Mathf.Sin(((float)Mathf.Abs(i)) / (float)riverWidth[flowGraphIndex]);
        //}

    }

    private void GenerateFlowingGraph(int sourceIndex)
    {
        List<Vector2Int> riverPoints = new List<Vector2Int>
        {
            new Vector2Int(sourceCoordinates[sourceIndex].x, sourceCoordinates[sourceIndex].y)
        };
        List<int> exploredIndexes = new List<int>
        {
            sourceCoordinates[sourceIndex].x+ sourceCoordinates[sourceIndex].y*(xSize+1)
        };
        while(riverPoints.Count!=0)
        {
            float currentHeight = vertices[riverPoints[0].x+ riverPoints[0].y*(xSize+1)].y;
            int nextIndexX = -1;
            int nextIndexZ = -1;
            Debug.Log(riverPoints[0].x + riverPoints[0].y * (xSize + 1));
            for(int i = -1 ; i <=  1 ; i++)
            {
                for (int j = - 1 ; j <= 1 ; j++)
                {
                    int index = riverPoints[0].x + i + ((riverPoints[0].y + j) * (xSize + 1));
                    
                    if(i != 0 && j != 0 && riverPoints[0].x + i + ((riverPoints[0].y + j) * (xSize + 1))>=0 && riverPoints[0].x + i + ((riverPoints[0].y + j) * (xSize + 1))< (xSize+1)*(zSize+1) )
                    {
                        if(currentHeight + downhillHeightTolerance > vertices[riverPoints[0].x + i + ((riverPoints[0].y + j) * (xSize + 1))].y && !exploredIndexes.Contains(riverPoints[0].x + i + ((riverPoints[0].y + j) * (xSize + 1))))
                        {                      
                            currentHeight = vertices[index].y;
                            nextIndexX = riverPoints[0].x + i ;
                            nextIndexZ = (riverPoints[0].y + j);
                        }

                    }

                }
            }
            if (nextIndexZ!=-1)
            {
                riverPoints.RemoveAt(0);
                riverPoints.Add(new Vector2Int(nextIndexX,nextIndexZ));
                flowGraphs[0].Add(nextIndexX+nextIndexZ * (xSize + 1));
                exploredIndexes.Add(nextIndexX+ nextIndexZ * (xSize + 1));
            }
            else
            {
                break;
            }
        }
    }

    private Texture2D GenerateColorTexture()
     {
        Color[] colourMap = new Color[(xSize+1) * (zSize+1)];

        Texture2D texture = new Texture2D(xSize, zSize);
        texture.filterMode = FilterMode.Point;
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float currentHeight = vertices[i].y;
                for(int j=0 ;j<regions.Length;j++)
                {
                    if(currentHeight <= regions[j].height)
                    {
                        colourMap[i] = regions[j].colour;
                        break;
                    }
                }
                i++;
            }
        }
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
     }

    private Texture GenerateTexture()
    {
        texture = new Texture2D(xSize, zSize);

        for(int x=0; x < xSize; x++)
        {
            for(int z = 0;z< zSize;z++)
            {
                Color color = CalculateColor(x, z);
                texture.SetPixel(x, z, color);
            }
        }
        return texture;
    }

    private Color CalculateColor(int x, int z)
    {
        float xCoord = ((float)x / (float)xSize * secondaryScale) + secondaryOffsetX;
        float zCoord = ((float)z / (float)zSize * secondaryScale) + secondaryOffsetZ;

        float sample = Mathf.PerlinNoise(xCoord, zCoord);
        return new Color(sample, sample, sample);
    }

    void Update()
    {
        //renderr.material.mainTexture = GenerateTexture();
        //UpdateMesh();
    }

    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for(int i = 0, z = 0 ; z<=zSize ;z++)
        {
            for(int x = 0 ; x <= xSize ;x++)
            {

                float y = Mathf.PerlinNoise(((float)x/(float)xSize) * mainScale + mainOffsetX, ((float)z / (float)zSize * mainScale) + mainOffsetZ) * mainNoiseMultiplicator;
                if(y > maxHeight)
                {
                    y = maxHeight;
                }
                else if(y < minHeight)
                {
                    y = minHeight;
                }
                vertices[i] = new Vector3(x, y + (texture.GetPixel(x,z).r * secondaryNoiseMultiplicator) , z);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];

        int vert = 0;
        int tris = 0;

        for(int z =0;z<zSize;z++)
        {
            for(int x =0;x<xSize;x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize +1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize +1;
                triangles[tris + 5] = vert + xSize +2;

                vert++;
                tris += 6;
            }
            vert++;
        }       
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        Vector2[] uvs = new Vector2[vertices.Length];
        int i = 0;
        while (i < uvs.Length)
        {
            uvs[i] = new Vector2(vertices[i].x/xSize, vertices[i].y / zSize );
            i++;
        }
        mesh.uv = uvs;
    }

    private void OnDrawGizmos()
    {

        if (vertices == null)
            return;

        for(int i=0;i<vertices.Length;i++)
        {
            Gizmos.DrawSphere(vertices[i], .1f);
        }
    }
    
    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color colour;
    }
}
