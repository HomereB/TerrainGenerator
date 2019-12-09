using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;



    public int seed;

    public int xSize =256;
    public int zSize = 256;
    public int maxHeight;
    public int minHeight;

    public float mainOffsetX;
    public float mainOffsetZ;

    public float secondaryOffsetX;
    public float secondaryOffsetZ;

    public float mainScale = 1;
    public float secondaryScale = 1;

    public float mainNoiseMultiplicator;
    public float secondaryNoiseMultiplicator;

    Renderer renderr;
    Texture2D texture;

    System.Random rng;

    public TerrainType[] regions;


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
        UpdateMesh();
        renderr.material.mainTexture = GenerateColorTexture();      
    }

     private Texture2D GenerateColorTexture()
     {
        Color[] colourMap = new Color[xSize * zSize];

        Texture2D texture = new Texture2D(xSize, zSize);
        texture.filterMode = FilterMode.Point;
        for (int i = 0, z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
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
        texture.Apply();
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
