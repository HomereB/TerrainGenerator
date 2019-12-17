using System;
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
    public int xSize = 256;
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
    public List<Vector2Int> sourceCoordinates;
    public float downhillHeightTolerance;
    public List<List<int>> flowGraphs;
    public List<List<int>> riverNodes;

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

        flowGraphs = new List<List<int>>(sourceCoordinates.Count);
        riverNodes = new List<List<int>>(sourceCoordinates.Count);

        for (int i = 0; i<sourceCoordinates.Count;i++)
        {
            flowGraphs.Add(new List<int>());
            riverNodes.Add(new List<int>());
            GenerateFlowingGraph(i);
        }

        for (int i =0; i< flowGraphs.Count;i++)
        {
            GenerateRiverDepth(i);
        }

        renderr.sharedMaterial.mainTexture = GenerateColorTexture();
        GetComponent<MeshFilter>().mesh.RecalculateBounds();
        UpdateMesh();
    }

    private void GenerateRiverDepth(int flowGraphIndex)
    {
        Dictionary<int,int> nearbyNodes = new Dictionary<int, int>();
        Dictionary<int, Vector3> nodesSlopes = new Dictionary<int, Vector3>();
        int MaxValue = 0;
        int MaxIndex = -1;
        foreach (int index in flowGraphs[flowGraphIndex])
        { 
            for(int i = -riverWidth[flowGraphIndex] ;i<= riverWidth[flowGraphIndex];i++)
            {
                for (int j = -riverWidth[flowGraphIndex]; j <= riverWidth[flowGraphIndex]; j++)
                {
                    if (index + i + (j * (xSize + 1)) >= 0 && index + i + (j * (xSize + 1)) < (xSize + 1) * (zSize + 1))
                    {
                        if (!nearbyNodes.ContainsKey(index+j + i*(xSize+1)))
                        {
                            int tempIndex = index + j + i * (xSize + 1);
                            nearbyNodes.Add(tempIndex, 1);

                            Vector3 slope = Vector3.zero;

                            for (int k = -1; k <= 1; k++)
                            {
                                for (int l = -1; l <= 1; l++)
                                {
                                    if (k != 0 && l != 0 && tempIndex + k + (l * (xSize + 1)) >= 0 && tempIndex + k + (l * (xSize + 1)) < (xSize + 1) * (zSize + 1))
                                    {
                                        slope += vertices[tempIndex + k + (l * (xSize + 1))] - vertices[tempIndex];
                                    }
                                }
                            }
                            nodesSlopes.Add(tempIndex, slope );
                        }
                        else
                        {
                            nearbyNodes[index + j + i * (xSize + 1)] +=1;
                        }
                        if (nearbyNodes[index + j + i * (xSize + 1)]>MaxValue)
                        {
                            MaxIndex = index + j + i * (xSize + 1);
                            MaxValue = nearbyNodes[index + j + i * (xSize + 1)];
                        }
                    }

                }
            }
        }

        riverNodes[flowGraphIndex] = new List<int>(nearbyNodes.Keys);

        foreach (int index in nearbyNodes.Keys)
        {
            vertices[index] += nodesSlopes[index] - new Vector3(0,riverDepth[flowGraphIndex] * nearbyNodes[index] / MaxValue,0);
        }
    }

    private void GenerateFlowingGraph(int sourceIndex)
    {
        List<Vector2Int> riverPoints = new List<Vector2Int>
        {
            new Vector2Int(sourceCoordinates[sourceIndex].x, sourceCoordinates[sourceIndex].y)
        };

        List<int> exploredIndexes = new List<int>
        {
            sourceCoordinates[sourceIndex].x + sourceCoordinates[sourceIndex].y * (xSize+1)
        };

        while(riverPoints.Count!=0)
        {
            float currentHeight = vertices[riverPoints[0].x + riverPoints[0].y * (xSize + 1)].y;
            int nextIndexX = -1;
            int nextIndexZ = -1;
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
                flowGraphs[sourceIndex].Add(nextIndexX+nextIndexZ * (xSize + 1));
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
        Color[] colourMap = new Color[vertices.Length];

        Texture2D texture = new Texture2D(xSize + 1, zSize + 1);
        texture.filterMode = FilterMode.Point;
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float currentHeight = vertices[i].y;
                for(int j=0 ; j<regions.Length; j++)
                {
                    if(currentHeight <= regions[j].height)
                    {
                        texture.SetPixel(x, z, regions[j].colour);
                        break;
                    }
                }
                i++;
            }
        }
        for (int i = 0; i < sourceCoordinates.Count; i++)
        {
            for (int j = 0; j < riverNodes[i].Count; j++)
            {
                texture.SetPixel(riverNodes[i][j] % (xSize + 1), riverNodes[i][j] / (xSize + 1), regions[0].colour);
            }
        }
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
            uvs[i] = new Vector2(vertices[i].x/ (float) (xSize + 1), vertices[i].z / (float)(zSize + 1) );
            i++;
        }
        mesh.uv = uvs;
    }

    /*private void OnDrawGizmos()
    {

        if (vertices == null)
            return;

        for(int i=0;i<vertices.Length;i++)
        {
            Gizmos.DrawSphere(vertices[i], .1f);
        }
    }*/
    
    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color colour;
    }
}
