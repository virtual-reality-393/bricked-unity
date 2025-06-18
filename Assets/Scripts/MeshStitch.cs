using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshStitch : MonoBehaviour
{
    private MeshFilter[] _meshFilters;
    public const float StudLength = 0.065f/4f;
    public const float StudHeight = 0.0208f;
    public Mesh studMesh;
    public (Vector3, int, int)[] gridPositions;
    
    List<int> triangles = new List<int>();
    List<Vector3> vertices = new List<Vector3>();
    List<Color> colors = new List<Color>();
    
    private int buildIdx;

    public (int,int)[,,] voxelGrid;

    private Vector3Int[] neighbours;

    public (Vector3Int, Vector3[])[] faces;

    public Color[] studToColor = new Color[]
    { 
        new Color(0, 0, 0, 0), new Color(0.75f, 0, 0), new Color(0.05f, 0.05f, 0.9f), new Color(0, 0.75f, 0),  new Color(0.9f, 0.78f, 0)
    };

    private Mesh _meshTest;
    private Vector3 _bounds;
    private Vector3 _offset;

    void Awake()
    {
        var rotation = transform.rotation;
        transform.rotation = Quaternion.identity;
        faces = new (Vector3Int, Vector3[])[6];
        
        // Right face (+X)
        faces[0] = (Vector3Int.right, new[]
        {
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f,  0.5f),
            new Vector3(0.5f,  0.5f, -0.5f),
            new Vector3(0.5f,  0.5f,  0.5f),
        });

// Left face (-X)
        faces[1] = (Vector3Int.left, new[]
        {
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
        });

// Up face (+Y)
        faces[2] = (Vector3Int.up, new[]
        {
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3( 0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f,  0.5f),
            new Vector3( 0.5f, 0.5f,  0.5f),
        });

// Down face (-Y)
        faces[3] = (Vector3Int.down, new[]
        {
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
        });

// Forward face (+Z)
        faces[4] = (Vector3Int.forward, new[]
        {
            new Vector3( 0.5f, -0.5f, 0.5f), // 0 - Bottom Right
            new Vector3(-0.5f, -0.5f, 0.5f), // 1 - Bottom Left
            new Vector3( 0.5f,  0.5f, 0.5f), // 2 - Top Right
            new Vector3(-0.5f,  0.5f, 0.5f), // 3 - Top Left
        });
        faces[5] = (Vector3Int.back, new[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f), // Bottom Left
            new Vector3( 0.5f, -0.5f, -0.5f), // Bottom Right
            new Vector3(-0.5f,  0.5f, -0.5f), // Top Left
            new Vector3( 0.5f,  0.5f, -0.5f), // Top Right
        });

        
        neighbours = new[]{ Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, Vector3Int.forward, Vector3Int.back };
        
        List<(Vector3,int,int)> positions = new List<(Vector3,int,int)>();
        Vector3 minBound = Vector3.positiveInfinity;
        Vector3 maxBound = Vector3.negativeInfinity;
        for (int i = 0; i < transform.childCount; i++)
        {
            
            var child = transform.GetChild(i);
            
            child.gameObject.SetActive(false);
            for (int x = -2; x < 2; x++)
            {
                for (int y = -1; y < 1; y++)
                {
                    Vector3 pos = child.localPosition - child.right * ((x + 0.5f) * StudLength) +
                                  child.forward * ((y + 0.5f) * StudLength);
                    pos.x /= StudLength;
                    pos.y /= StudHeight;
                    pos.z /= StudLength;
                    minBound = Vector3.Min(minBound, pos);
                    maxBound = Vector3.Max(maxBound, pos);
                    var color= 0 ;
                    if (child.name.Contains("Red"))
                    {
                        color = 1;
                    }
                    else if (child.name.Contains("Blue"))
                    {
                        color = 2;
                    }
                    else if (child.name.Contains("Green"))
                    {
                        color = 3;
                    }
                    else if (child.name.Contains("Yellow"))
                    {
                        color = 4;
                    }
                    positions.Add((pos,color,i));
                }
            }
        }
        
        // gameObject.SetActive(false);
        
        

        _bounds = maxBound - minBound + Vector3.one;

        _offset = minBound;


        voxelGrid = new (int,int)[(int)_bounds.x, (int)_bounds.y, (int)_bounds.z];

        foreach (var pos in positions)
        {
            var newPos = pos.Item1 - minBound;
            voxelGrid[Mathf.RoundToInt(newPos.x),Mathf.RoundToInt(newPos.y),Mathf.RoundToInt(newPos.z)] = (pos.Item2,pos.Item3);
        }

        _meshTest = new Mesh();
        _meshTest.MarkDynamic();
        gridPositions = positions.ToArray();
        GetComponent<MeshFilter>().mesh = _meshTest;
        // CreateMesh(10000);
        
        transform.rotation = rotation;
    }
    
    bool IsOutsideOfBounds(int i, int j, int k, Vector3 bounds)
    {
        return i < 0 || i >= bounds.x-1e-4 || j < 0 || j >= bounds.y-1e-4 || k < 0 || k >= bounds.z-1e-4;
    }
    


    public void CreateMesh(int currIdx)
    {
        
        var meshOffset = new Vector3(_offset.x * StudLength, _offset.y*StudHeight,_offset.z*StudLength);
        int vertexCount = 0;
        foreach (var v in gridPositions)
        {
            if (v.Item3 > currIdx) break;
            var newPos = v.Item1 - _offset;
            int i = Mathf.RoundToInt(newPos.x);
            int j = Mathf.RoundToInt(newPos.y);
            int k = Mathf.RoundToInt(newPos.z);

            if (voxelGrid[i, j, k].Item1 != 0)
            {
                foreach (var (idx,faceArray) in faces)
                {
                    if (IsOutsideOfBounds(i+idx.x,j+idx.y,k+idx.z,_bounds) || voxelGrid[i+idx.x, j+idx.y, k+idx.z].Item1 == 0 || voxelGrid[i+idx.x, j+idx.y, k+idx.z].Item2 > currIdx)
                    {
                        triangles.Add(0 + vertexCount);
                        triangles.Add(2 + vertexCount);
                        triangles.Add(1 + vertexCount);
                        triangles.Add(2 + vertexCount);
                        triangles.Add(3 + vertexCount);
                        triangles.Add(1 + vertexCount);
                                
                        foreach (var vertex in faceArray)
                        {
                            var newVertex =
                                new Vector3((i + vertex.x) * StudLength, (j + vertex.y) * StudHeight,
                                    (k + vertex.z) * StudLength)+meshOffset;
                            
                            vertices.Add(newVertex);
                            vertexCount++;
                                    
                            colors.Add(studToColor[voxelGrid[i, j, k].Item1]);
                                    
                                    
                        }
                        
                        if (idx == Vector3Int.up)
                        {
                            foreach (var triangle in studMesh.triangles)
                            {
                                triangles.Add(triangle+vertexCount);
                                
                            }
                            foreach (var vertex in studMesh.vertices)
                            {
                                var newVertex =
                                    new Vector3(vertex.x + i * StudLength, vertex.y + j * StudHeight + StudHeight * 1f/2f,
                                        vertex.z + k * StudLength)+meshOffset;
                                vertices.Add(newVertex);
                                vertexCount++;
                                colors.Add(studToColor[voxelGrid[i, j, k].Item1]);
                            }

                            
                        }
                    }
                }
            }
        }
        
        _meshTest.Clear();
        _meshTest.vertices = vertices.ToArray();
        _meshTest.triangles = triangles.ToArray();
        _meshTest.colors = colors.ToArray();
        _meshTest.RecalculateNormals();
        
        triangles.Clear();
        vertices.Clear();
        colors.Clear();
    }
}

public enum Stud
{
    None,Red,Green,Blue,Yellow
}
