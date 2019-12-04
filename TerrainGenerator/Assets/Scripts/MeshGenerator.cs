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

    void Start()
    {
       rng = new System.Random(seed);

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        renderr = GetComponent<Renderer>();
        renderr.material.mainTexture = GenerateTexture();
        CreateShape();
    }

    private Texture GenerateTexture()
    {
        texture = new Texture2D(xSize, zSize);

        for(int x=0; x < xSize; x++)
        {
            for(int z = 0;z< zSize;z++)
            {
                secondaryOffsetX = (float)rng.Next(-100000, 100000)/1000000.0f;
                secondaryOffsetZ = (float)rng.Next(-100000, 100000) / 1000000.0f;
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
        UpdateMesh();
    }

    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for(int i = 0, z = 0 ; z<=zSize ;z++)
        {
            for(int x = 0 ; x <= xSize ;x++)
            {
                mainOffsetX = (float)rng.Next(-100000, 100000) / 10000000.0f;
                mainOffsetZ = (float)rng.Next(-100000, 100000) / 10000000.0f;
                float y = Mathf.PerlinNoise(((float)x/(float)xSize  + mainOffsetX) * mainScale, ((float)z / (float)zSize * mainScale)  + mainOffsetZ) * mainNoiseMultiplicator;
                Debug.Log(y);
                vertices[i] = new Vector3(x, maxHeight*y + (texture.GetPixel(x,z).r * secondaryNoiseMultiplicator) , z);
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
}
